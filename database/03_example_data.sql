-- Sample salon data based on the mockup. Existing services and prices are preserved.
insert into public.mw_services(category,name,duration_minutes,price)
select v.category,v.name,v.minutes,v.price from (values
 ('Маникюр','Маникюр классический',60,1800),('Волосы','Стрижка и укладка',90,2800),
 ('Массаж','Массаж расслабляющий',60,3200),('Лицо','Уход за лицом',75,3500),
 ('Маникюр','Маникюр с покрытием',90,2600),('Маникюр','Педикюр классический',75,3500),('Маникюр','Педикюр + гель-лак',110,5000),
 ('Маникюр','Снятие покрытия',30,900),('Маникюр','Укрепление ногтей',30,1200),
 ('Волосы','Окрашивание AirTouch',150,6500),('Брови и ресницы','Наращивание ресниц',90,3200),
 ('Брови и ресницы','Коррекция бровей',30,1200),('Лицо','SPA-уход',60,3000)
) v(category,name,minutes,price) where not exists(select 1 from public.mw_services s where s.name=v.name);
update public.mw_services set category='Лицо' where category='Косметология';
update public.mw_services s set master_id=m.id from public.mw_staff m where s.master_id is null and m.username=case s.category when 'Маникюр' then 'elena' when 'Волосы' then 'maria' when 'Массаж' then 'alina' when 'Брови и ресницы' then 'irina' when 'Лицо' then case when s.name='SPA-уход' then 'anna' else 'olga' end end;
insert into public.mw_clients(name,phone,email) values
 ('Анна Иванова','+7 925 123-45-67','anna.ivanova@example.com'),
 ('Ксения Петрова','+7 916 987-65-43','ksenia@example.com'),
 ('Дарья Смирнова','+7 903 111-22-33','d.smirnova@example.com'),
 ('София Николаева','+7 926 444-56-78','sofia@example.com'),
 ('Мария Кузнецова','+7 915 333-21-09','maria.k@example.com'),
 ('Екатерина Орлова','+7 909 876-54-32','ekaterina@example.com') on conflict(phone_digits) do nothing;
with seed(client_name,service_name,master_login,time_text,status) as (values
 ('Анна Иванова','Маникюр классический','elena','10:00','completed'),
 ('Ксения Петрова','Уход за лицом','olga','11:30','completed'),
 ('Дарья Смирнова','Окрашивание AirTouch','maria','12:00','waiting'),
 ('Мария Кузнецова','Наращивание ресниц','irina','12:30','confirmed'),
 ('Екатерина Орлова','Массаж расслабляющий','alina','13:00','confirmed'),
 ('Анна Иванова','Маникюр с покрытием','elena','13:30','confirmed'),
 ('Ксения Петрова','Уход за лицом','olga','14:00','in_progress'),
 ('Дарья Смирнова','Стрижка и укладка','maria','14:30','waiting'),
 ('Мария Кузнецова','Наращивание ресниц','irina','15:00','confirmed')
), prepared as (
 select c.id as client_id,s.id as service_id,m.id as master_id,
 ((now() at time zone 'Europe/Moscow')::date+seed.time_text::time) at time zone 'Europe/Moscow' as starts_at,
 s.duration_minutes,s.price,seed.status
 from seed join public.mw_clients c on c.name=seed.client_name join public.mw_services s on s.name=seed.service_name join public.mw_staff m on m.username=seed.master_login
)
insert into public.mw_appointments(client_id,service_id,master_id,starts_at,ends_at,price,status)
select p.client_id,p.service_id,p.master_id,p.starts_at,p.starts_at+make_interval(mins=>p.duration_minutes),p.price,p.status
from prepared p where not exists(select 1 from public.mw_appointments a where a.master_id=p.master_id and a.starts_at=p.starts_at);
