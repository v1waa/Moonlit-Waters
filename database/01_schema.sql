-- Moonlit Waters: migration of the existing mw_* tables.
-- The application uses username/password sessions and checked RPC calls.
create schema if not exists moonlit_private;
revoke all on schema moonlit_private from public, anon, authenticated;
create extension if not exists pgcrypto with schema extensions;
create extension if not exists btree_gist with schema extensions;

alter table public.mw_staff
  add column if not exists specialization text not null default '',
  add column if not exists bank_account text not null default '',
  add column if not exists work_start time not null default '10:00',
  add column if not exists work_end time not null default '19:00',
  add column if not exists is_day_off boolean not null default false;
alter table public.mw_staff add constraint mw_work_hours_check check (work_start < work_end);
alter table public.mw_clients add column if not exists email text not null default '';
alter table public.mw_clients add column if not exists phone_digits text generated always as (regexp_replace(phone, '[^0-9]', '', 'g')) stored;
create unique index if not exists mw_clients_phone_unique on public.mw_clients(phone_digits);
alter table public.mw_services
  add column if not exists master_id uuid references public.mw_staff(id),
  add column if not exists is_active boolean not null default true;
alter table public.mw_appointments
  add column if not exists ends_at timestamptz,
  add column if not exists price numeric(12,2) not null default 0;
update public.mw_appointments a set ends_at=a.starts_at+make_interval(mins=>s.duration_minutes), price=s.price
  from public.mw_services s where s.id=a.service_id and a.ends_at is null;
alter table public.mw_appointments alter column ends_at set not null;
alter table public.mw_appointments drop constraint if exists mw_appointments_status_check;
update public.mw_appointments set status='confirmed' where status='scheduled';
alter table public.mw_appointments alter column status set default 'confirmed';
alter table public.mw_appointments add constraint mw_appointments_status_check
  check(status in ('waiting','confirmed','in_progress','completed','cancelled'));
alter table public.mw_appointments add constraint mw_appointment_time_check check(ends_at>starts_at);
alter table public.mw_appointments add constraint mw_no_overlap exclude using gist
  (master_id with =, tstzrange(starts_at,ends_at,'[)') with &&) where (status <> 'cancelled');
create index if not exists mw_appointments_client_idx on public.mw_appointments(client_id);
create index if not exists mw_appointments_service_idx on public.mw_appointments(service_id);
create index if not exists mw_appointments_day_idx on public.mw_appointments(starts_at);
create index if not exists mw_sessions_account_idx on public.mw_sessions(account_id);
create index if not exists mw_services_master_idx on public.mw_services(master_id);

-- No direct table access: each RPC verifies the session and role on the server.
revoke all on public.mw_staff,public.mw_sessions,public.mw_clients,public.mw_services,public.mw_appointments from anon,authenticated;
do $$ begin
 if to_regprocedure('public.mw_authenticate(text,text)') is not null then
  revoke execute on function public.mw_authenticate(text,text) from public,anon,authenticated;
 end if;
end $$;

create or replace function moonlit_private.require_session(p_token text)
returns public.mw_staff language plpgsql security definer set search_path='' as $$
declare v_user public.mw_staff;
begin
  if p_token is null or length(p_token)<>64 then raise exception 'Сеанс завершён. Войдите снова.'; end if;
  select s.* into v_user from public.mw_staff s join public.mw_sessions t on t.account_id=s.id
    where t.token_hash=encode(extensions.digest(p_token,'sha256'),'hex') and t.expires_at>now() and s.is_active;
  if not found then raise exception 'Сеанс завершён. Войдите снова.'; end if;
  return v_user;
end $$;
revoke all on function moonlit_private.require_session(text) from public,anon,authenticated;

create or replace function public.mw_login(p_username text,p_password text)
returns jsonb language plpgsql security definer set search_path='' as $$
declare a public.mw_staff; v_token text;
begin
  if coalesce(length(p_username),0) not between 3 and 32 or coalesce(length(p_password),0) not between 1 and 72 then
    return jsonb_build_object('ok',false,'message','Проверьте логин и пароль.');
  end if;
  select * into a from public.mw_staff where username=lower(trim(p_username)) for update;
  if not found or not a.is_active or a.locked_until>now() then
    return jsonb_build_object('ok',false,'message','Неверный логин или пароль. После 5 ошибок подождите 15 минут.');
  end if;
  if extensions.crypt(p_password,a.password_hash) is distinct from a.password_hash then
    update public.mw_staff set failed_attempts=case when a.failed_attempts>=4 then 0 else a.failed_attempts+1 end,
      locked_until=case when a.failed_attempts>=4 then now()+interval '15 minutes' else null end where id=a.id;
    return jsonb_build_object('ok',false,'message','Неверный логин или пароль. После 5 ошибок подождите 15 минут.');
  end if;
  update public.mw_staff set failed_attempts=0,locked_until=null where id=a.id;
  delete from public.mw_sessions where expires_at<now();
  v_token:=encode(extensions.gen_random_bytes(32),'hex');
  insert into public.mw_sessions(token_hash,account_id,expires_at)
    values(encode(extensions.digest(v_token,'sha256'),'hex'),a.id,now()+interval '12 hours');
  return jsonb_build_object('ok',true,'token',v_token,'profile',jsonb_build_object('id',a.id,'username',a.username,'full_name',a.full_name,'role',a.role));
end $$;

create or replace function public.mw_logout(p_token text)
returns boolean language plpgsql security definer set search_path='' as $$
begin
  delete from public.mw_sessions where token_hash=encode(extensions.digest(p_token,'sha256'),'hex');
  return true;
end $$;

create or replace function public.mw_change_password(p_token text,p_old_password text,p_new_password text)
returns boolean language plpgsql security definer set search_path='' as $$
declare a public.mw_staff;
begin
  a:=moonlit_private.require_session(p_token);
  if p_old_password is null or extensions.crypt(p_old_password,a.password_hash) is distinct from a.password_hash then raise exception 'Текущий пароль неверен.'; end if;
  if coalesce(length(p_new_password),0)<10 or octet_length(p_new_password)>72 then raise exception 'Новый пароль: от 10 символов, не более 72 байт.'; end if;
  update public.mw_staff set password_hash=extensions.crypt(p_new_password,extensions.gen_salt('bf',12)) where id=a.id;
  delete from public.mw_sessions where account_id=a.id and token_hash<>encode(extensions.digest(p_token,'sha256'),'hex');
  return true;
end $$;

create or replace function public.mw_load(p_token text,p_day date)
returns jsonb language plpgsql security definer set search_path='' as $$
declare a public.mw_staff; result jsonb;
begin
  a:=moonlit_private.require_session(p_token);
  select jsonb_build_object(
    'clients',coalesce((select jsonb_agg(to_jsonb(c) order by c.name) from (
      select c.id,c.name,c.phone,c.email,
        (select count(*) from public.mw_appointments v where v.client_id=c.id and v.status='completed' and (a.role='admin' or v.master_id=a.id)) as visits,
        last_visit.starts_at as last_visit,last_visit.service_name as last_service,last_visit.master_name as last_master
      from public.mw_clients c left join lateral (
        select v.starts_at,s.name as service_name,m.full_name as master_name from public.mw_appointments v
        join public.mw_services s on s.id=v.service_id join public.mw_staff m on m.id=v.master_id
        where v.client_id=c.id and v.status='completed' and (a.role='admin' or v.master_id=a.id)
        order by v.starts_at desc limit 1
      ) last_visit on true
      where a.role='admin' or exists(select 1 from public.mw_appointments v where v.client_id=c.id and v.master_id=a.id)
    ) c),'[]'::jsonb),
    'services',coalesce((select jsonb_agg(to_jsonb(s) order by s.category,s.name) from public.mw_services s
      where s.is_active and (a.role='admin' or s.master_id is null or s.master_id=a.id)),'[]'::jsonb),
    'staff',coalesce((select jsonb_agg(to_jsonb(m) order by m.full_name) from (
      select id,username,full_name,role,phone,specialization,bank_account,work_start,work_end,is_day_off
      from public.mw_staff where is_active and role='master' and (a.role='admin' or id=a.id)
    ) m),'[]'::jsonb),
    'appointments',coalesce((select jsonb_agg(to_jsonb(v) order by v.starts_at) from (
      select v.*,c.name as client_name,c.phone as client_phone,s.name as service_name,m.full_name as master_name
      from public.mw_appointments v join public.mw_clients c on c.id=v.client_id
      join public.mw_services s on s.id=v.service_id join public.mw_staff m on m.id=v.master_id
      where (v.starts_at at time zone 'Europe/Moscow')::date=p_day and (a.role='admin' or v.master_id=a.id)
    ) v),'[]'::jsonb)
  ) into result;
  return result;
end $$;

create or replace function public.mw_save_client(p_token text,p_id uuid,p_name text,p_phone text,p_email text)
returns uuid language plpgsql security definer set search_path='' as $$
declare a public.mw_staff; v_id uuid;
begin
  a:=moonlit_private.require_session(p_token);
  if a.role<>'admin' then raise exception 'Доступно только администратору.'; end if;
  if coalesce(length(trim(p_name)),0) not between 2 and 150 then raise exception 'Укажите ФИО клиента.'; end if;
  if coalesce(length(regexp_replace(p_phone,'[^0-9]','','g')),0) not between 10 and 15 then raise exception 'Укажите корректный телефон.'; end if;
  if length(coalesce(p_email,''))>0 and p_email !~ '^[^ @]+@[^ @]+\.[^ @]+$' then raise exception 'Проверьте email.'; end if;
  if p_id is null then
    insert into public.mw_clients(name,phone,email) values(trim(p_name),trim(p_phone),trim(coalesce(p_email,''))) returning id into v_id;
  else
    update public.mw_clients set name=trim(p_name),phone=trim(p_phone),email=trim(coalesce(p_email,'')) where id=p_id returning id into v_id;
    if not found then raise exception 'Клиент не найден.'; end if;
  end if;
  return v_id;
exception when unique_violation then raise exception 'Клиент с таким телефоном уже существует.';
end $$;

create or replace function public.mw_save_service(p_token text,p_id uuid,p_name text,p_category text,p_duration integer,p_price numeric,p_master_id uuid)
returns uuid language plpgsql security definer set search_path='' as $$
declare a public.mw_staff; v_id uuid;
begin
  a:=moonlit_private.require_session(p_token);
  if a.role<>'admin' then raise exception 'Доступно только администратору.'; end if;
  if coalesce(length(trim(p_name)),0) not between 2 and 150 or coalesce(length(trim(p_category)),0)=0 then raise exception 'Укажите название и категорию.'; end if;
  if p_duration is null or p_duration not between 5 and 720 or p_price is null or p_price<0 or p_price>1000000 then raise exception 'Проверьте время и стоимость.'; end if;
  if p_master_id is not null and not exists(select 1 from public.mw_staff where id=p_master_id and is_active and role='master') then raise exception 'Мастер недоступен.'; end if;
  if p_id is null then
    insert into public.mw_services(name,category,duration_minutes,price,master_id) values(trim(p_name),p_category,p_duration,p_price,p_master_id) returning id into v_id;
  else
    update public.mw_services set name=trim(p_name),category=p_category,duration_minutes=p_duration,price=p_price,master_id=p_master_id where id=p_id and is_active returning id into v_id;
    if not found then raise exception 'Услуга не найдена.'; end if;
  end if;
  return v_id;
end $$;

create or replace function public.mw_save_master(p_token text,p_id uuid,p_name text,p_phone text,p_specialization text,p_bank_account text,p_work_start time,p_work_end time,p_day_off boolean)
returns jsonb language plpgsql security definer set search_path='' as $$
declare a public.mw_staff; v_id uuid; v_login text; v_password text;
begin
  a:=moonlit_private.require_session(p_token);
  if a.role<>'admin' then raise exception 'Доступно только администратору.'; end if;
  if coalesce(length(trim(p_name)),0) not between 2 and 150 or coalesce(length(trim(p_specialization)),0)=0 then raise exception 'Укажите ФИО и специализацию.'; end if;
  if coalesce(length(regexp_replace(p_phone,'[^0-9]','','g')),0) not between 10 and 15 then raise exception 'Укажите корректный телефон.'; end if;
  if coalesce(p_bank_account,'')<>'' and p_bank_account !~ '^[0-9]{20}$' then raise exception 'Банковский счёт должен содержать 20 цифр или быть пустым.'; end if;
  if p_work_start is null or p_work_end is null or p_work_start>=p_work_end then raise exception 'Проверьте часы работы.'; end if;
  if p_id is null then
    v_login:='master_'||encode(extensions.gen_random_bytes(5),'hex');
    v_password:=encode(extensions.gen_random_bytes(9),'hex');
    insert into public.mw_staff(username,full_name,role,phone,password_hash,specialization,bank_account,work_start,work_end,is_day_off)
      values(v_login,trim(p_name),'master',p_phone,extensions.crypt(v_password,extensions.gen_salt('bf',12)),trim(p_specialization),coalesce(p_bank_account,''),p_work_start,p_work_end,coalesce(p_day_off,false)) returning id into v_id;
  else
    update public.mw_staff set full_name=trim(p_name),phone=p_phone,specialization=trim(p_specialization),bank_account=coalesce(p_bank_account,''),work_start=p_work_start,work_end=p_work_end,is_day_off=coalesce(p_day_off,false)
      where id=p_id and role='master' and is_active returning id,username into v_id,v_login;
    if not found then raise exception 'Мастер не найден.'; end if;
  end if;
  return jsonb_build_object('id',v_id,'username',v_login,'password',v_password);
end $$;

create or replace function public.mw_save_appointment(p_token text,p_id uuid,p_client_name text,p_phone text,p_service_id uuid,p_master_id uuid,p_starts_at timestamptz,p_status text)
returns uuid language plpgsql security definer set search_path='' as $$
declare a public.mw_staff; m public.mw_staff; s public.mw_services; old public.mw_appointments;
  v_client uuid; v_id uuid; v_end timestamptz; v_price numeric;
begin
  a:=moonlit_private.require_session(p_token);
  if p_master_id is null or (a.role='master' and p_master_id<>a.id) then raise exception 'Нельзя управлять записями другого мастера.'; end if;
  select * into m from public.mw_staff where id=p_master_id and is_active and role='master';
  if not found then raise exception 'Выберите мастера.'; end if;
  if p_id is not null then
    select * into old from public.mw_appointments where id=p_id for update;
    if not found then raise exception 'Запись не найдена.'; end if;
    if a.role='master' and old.master_id<>a.id then raise exception 'Нельзя изменять чужую запись.'; end if;
  end if;
  select * into s from public.mw_services where id=p_service_id;
  if not found or (not s.is_active and (p_id is null or old.service_id<>p_service_id)) then raise exception 'Услуга недоступна.'; end if;
  if s.master_id is not null and s.master_id<>p_master_id then raise exception 'Эту услугу выполняет другой мастер.'; end if;
  if p_status is null or p_status not in ('waiting','confirmed','in_progress','completed','cancelled') then raise exception 'Выберите статус.'; end if;
  if p_starts_at is null then raise exception 'Укажите дату и время.'; end if;
  if p_id is not null and old.service_id=p_service_id then
    v_end:=p_starts_at+(old.ends_at-old.starts_at); v_price:=old.price;
  else v_end:=p_starts_at+make_interval(mins=>s.duration_minutes); v_price:=s.price;
  end if;
  if p_status<>'cancelled' and (p_id is null or old.starts_at<>p_starts_at or old.service_id<>p_service_id or old.master_id<>p_master_id) then
    if m.is_day_off or (p_starts_at at time zone 'Europe/Moscow')::time<m.work_start
      or (v_end at time zone 'Europe/Moscow')::time>m.work_end
      or (v_end at time zone 'Europe/Moscow')::date<>(p_starts_at at time zone 'Europe/Moscow')::date then
      raise exception 'Время записи выходит за часы работы мастера.';
    end if;
  end if;
  if coalesce(length(trim(p_client_name)),0) not between 2 and 150 or coalesce(length(regexp_replace(p_phone,'[^0-9]','','g')),0) not between 10 and 15 then raise exception 'Укажите ФИО и корректный телефон клиента.'; end if;
  insert into public.mw_clients(name,phone) values(trim(p_client_name),trim(p_phone)) on conflict(phone_digits) do nothing;
  select id into v_client from public.mw_clients where phone_digits=regexp_replace(p_phone,'[^0-9]','','g');
  if p_id is null then
    insert into public.mw_appointments(client_id,service_id,master_id,starts_at,ends_at,price,status)
      values(v_client,p_service_id,p_master_id,p_starts_at,v_end,v_price,p_status) returning id into v_id;
  else
    update public.mw_appointments set client_id=v_client,service_id=p_service_id,master_id=p_master_id,starts_at=p_starts_at,ends_at=v_end,price=v_price,status=p_status where id=p_id returning id into v_id;
  end if;
  return v_id;
exception when exclusion_violation then raise exception 'Это время у мастера уже занято. Выберите другое.';
end $$;

create or replace function public.mw_archive(p_token text,p_kind text,p_id uuid)
returns boolean language plpgsql security definer set search_path='' as $$
declare a public.mw_staff;
begin
  a:=moonlit_private.require_session(p_token);
  if a.role<>'admin' then raise exception 'Доступно только администратору.'; end if;
  if p_kind='service' then
    update public.mw_services set is_active=false where id=p_id;
  elsif p_kind='master' then
    if exists(select 1 from public.mw_appointments where master_id=p_id and ends_at>now() and status not in ('cancelled','completed')) then raise exception 'Сначала перенесите или отмените будущие записи мастера.'; end if;
    update public.mw_staff set is_active=false where id=p_id and role='master';
    delete from public.mw_sessions where account_id=p_id;
  else raise exception 'Неизвестный тип записи.';
  end if;
  return true;
end $$;

-- Only these checked endpoints are public. Helper functions remain private.
do $$ declare f record; begin
  for f in select p.oid::regprocedure as signature from pg_proc p join pg_namespace n on n.oid=p.pronamespace
    where n.nspname='public' and p.proname in ('mw_login','mw_logout','mw_change_password','mw_load','mw_save_client','mw_save_service','mw_save_master','mw_save_appointment','mw_archive')
  loop
    execute format('revoke all on function %s from public, anon, authenticated',f.signature);
    execute format('grant execute on function %s to anon, authenticated',f.signature);
  end loop;
end $$;
notify pgrst,'reload schema';
