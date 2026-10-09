-- Creates missing accounts only. Save the returned passwords: they are not stored in clear text.
with seed(username,full_name,role,phone,specialization,work_start,work_end,is_day_off) as (values
 ('admin','Мария К.','admin','+7 900 100-10-10','Администратор','10:00','19:00',false),
 ('elena','Елена Смирнова','master','+7 900 100-10-11','Маникюр','10:00','19:00',false),
 ('olga','Ольга Власова','master','+7 900 100-10-12','Косметолог','11:00','20:00',false),
 ('alina','Алина Романова','master','+7 900 100-10-13','Массаж','12:00','21:00',false),
 ('maria','Мария Климова','master','+7 900 100-10-14','Стилист','10:00','18:00',false),
 ('irina','Ирина Белова','master','+7 900 100-10-15','Брови и ресницы','10:00','19:00',false),
 ('anna','Анна Морозова','master','+7 900 100-10-16','SPA-уход','13:00','21:00',false)
), passwords as materialized (
 select seed.*, 'Mw!'||encode(extensions.gen_random_bytes(9),'hex') as password from seed
), inserted as (
 insert into public.mw_staff(username,full_name,role,phone,specialization,work_start,work_end,is_day_off,password_hash)
 select username,full_name,role,phone,specialization,work_start::time,work_end::time,is_day_off,extensions.crypt(password,extensions.gen_salt('bf',12)) from passwords
 on conflict(username) do nothing returning username
) select p.username,p.full_name,p.role,p.password from passwords p join inserted i using(username);
