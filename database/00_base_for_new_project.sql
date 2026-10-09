-- Only for a NEW Supabase project. The connected Moonlit Waters database is already configured.
create extension if not exists pgcrypto with schema extensions;
create table if not exists public.mw_staff (
 id uuid primary key default gen_random_uuid(),
 username text not null unique check(username ~ '^[a-z][a-z0-9_]{2,31}$'),
 full_name text not null check(length(trim(full_name))>0),
 role text not null check(role in ('admin','master')),
 phone text,password_hash text not null,failed_attempts integer not null default 0,
 locked_until timestamptz,is_active boolean not null default true,created_at timestamptz not null default now()
);
create table if not exists public.mw_clients (
 id uuid primary key default gen_random_uuid(),name text not null check(length(trim(name))>0),
 phone text not null,created_at timestamptz not null default now()
);
create table if not exists public.mw_services (
 id uuid primary key default gen_random_uuid(),category text not null,name text not null check(length(trim(name))>0),
 duration_minutes integer not null check(duration_minutes>0),price numeric(12,2) not null check(price>=0)
);
create table if not exists public.mw_appointments (
 id uuid primary key default gen_random_uuid(),client_id uuid not null references public.mw_clients(id),
 service_id uuid not null references public.mw_services(id),master_id uuid not null references public.mw_staff(id),
 starts_at timestamptz not null,status text not null default 'scheduled' check(status in ('scheduled','completed','cancelled')),
 created_at timestamptz not null default now()
);
create table if not exists public.mw_sessions (
 token_hash text primary key check(length(token_hash)=64),account_id uuid not null references public.mw_staff(id),
 expires_at timestamptz not null,created_at timestamptz not null default now()
);
alter table public.mw_staff enable row level security;
alter table public.mw_clients enable row level security;
alter table public.mw_services enable row level security;
alter table public.mw_appointments enable row level security;
alter table public.mw_sessions enable row level security;
