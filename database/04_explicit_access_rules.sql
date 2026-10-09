-- The desktop app authenticates its own username/password sessions in the RPC functions.
-- Direct REST access to these tables is explicitly denied.
do $$ declare t text; begin
 foreach t in array array['mw_staff','mw_sessions','mw_clients','mw_services','mw_appointments'] loop
   execute format('drop policy if exists desktop_no_direct_access on public.%I',t);
   execute format('create policy desktop_no_direct_access on public.%I for all to anon,authenticated using(false) with check(false)',t);
 end loop;
 if to_regprocedure('public.is_moonlit_admin()') is not null then
   revoke execute on function public.is_moonlit_admin() from public,anon;
   grant execute on function public.is_moonlit_admin() to authenticated;
 end if;
end $$;
