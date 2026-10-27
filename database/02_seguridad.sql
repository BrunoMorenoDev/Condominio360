-- =====================================================================
-- Condominio360 · 02_seguridad.sql
-- Seguridad de la base de datos en Supabase.
--
-- 1) Row Level Security (RLS) activo en todas las tablas y SIN políticas
--    para los roles 'anon' y 'authenticated': la API pública REST de
--    Supabase (PostgREST) no puede leer ni escribir nada.
-- 2) La aplicación ASP.NET Core se conecta con un rol propio de mínimo
--    privilegio (condominio_app) que solo puede SELECT/INSERT/UPDATE/DELETE
--    en estas tablas, con una política RLS que le da acceso completo.
--
-- IMPORTANTE: reemplace <CONTRASENA_SEGURA> antes de ejecutar. La contraseña
-- real NUNCA se sube al repositorio (va en appsettings.Local.json).
-- =====================================================================

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'condominio_app') THEN
        CREATE ROLE condominio_app LOGIN PASSWORD '<CONTRASENA_SEGURA>';
    END IF;
END $$;

GRANT USAGE ON SCHEMA public TO condominio_app;

DO $$
DECLARE
    t TEXT;
BEGIN
    FOREACH t IN ARRAY ARRAY[
        'roles','unidades','residentes','usuarios','configuracion_cobro',
        'cuotas','pagos','aplicaciones_pago','areas_comunales','reservas','visitas']
    LOOP
        EXECUTE format('ALTER TABLE public.%I ENABLE ROW LEVEL SECURITY', t);
        EXECUTE format('REVOKE ALL ON public.%I FROM anon, authenticated', t);
        EXECUTE format('GRANT SELECT, INSERT, UPDATE, DELETE ON public.%I TO condominio_app', t);
        EXECUTE format('DROP POLICY IF EXISTS app_acceso_total ON public.%I', t);
        EXECUTE format(
            'CREATE POLICY app_acceso_total ON public.%I AS PERMISSIVE FOR ALL TO condominio_app USING (true) WITH CHECK (true)', t);
    END LOOP;
END $$;

-- Las columnas identity necesitan permiso sobre sus secuencias
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO condominio_app;
