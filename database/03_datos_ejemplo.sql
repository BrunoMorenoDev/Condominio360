-- =====================================================================
-- Condominio360 · 03_datos_ejemplo.sql
-- Datos de ejemplo coherentes con el Core (generados con el mismo
-- algoritmo de recargo y distribución que usa la aplicación).
-- Parámetros: vencimiento día 10, 5 días de gracia, 1,5 % mensual.
-- Usuarios de prueba:  admin / Admin123*   ·   cmena / Residente123*
--                      atorres / Residente123*   ·   guardia / Guardia123*
-- =====================================================================

INSERT INTO public.roles (id, nombre, descripcion) VALUES
    (1, 'Administrador', 'Gestiona unidades, residentes, cobranza, áreas comunales y usuarios'),
    (2, 'Residente', 'Consulta su estado de cuenta, reporta pagos, reserva áreas y autoriza visitas'),
    (3, 'Guardia', 'Controla el ingreso y salida de visitas en la garita');

INSERT INTO public.unidades (id, torre, numero, piso, tipo, area_m2, alicuota, activa) VALUES
    (1, 'A', '101', 1, 'Departamento', 85.50, 72.00, TRUE),
    (2, 'A', '102', 1, 'Departamento', 92.00, 78.00, TRUE),
    (3, 'A', '201', 2, 'Departamento', 78.50, 95.00, TRUE),
    (4, 'A', '202', 2, 'Departamento', 110.00, 95.00, TRUE),
    (5, 'A', '301', 3, 'Departamento', 120.00, 110.00, TRUE),
    (6, 'B', '101', 1, 'Departamento', 70.00, 85.00, TRUE),
    (7, 'B', '102', 1, 'Departamento', 70.00, 85.00, TRUE),
    (8, 'B', '301', 3, 'Departamento', 64.30, 58.50, TRUE),
    (9, 'B', '302', 3, 'Departamento', 100.00, 90.00, TRUE),
    (10, 'C', '01', 0, 'Casa', 150.00, 120.00, TRUE),
    (11, 'C', '02', 0, 'Casa', 140.00, 100.00, TRUE),
    (12, 'PB', 'L-01', 0, 'Local', 40.00, 45.00, TRUE),
    (13, 'PB', 'L-02', 0, 'Local', 55.00, 120.00, TRUE),
    (14, 'PB', 'O-01', 0, 'Oficina', 35.00, 40.00, FALSE);

INSERT INTO public.residentes (id, nombres, apellidos, cedula, correo, telefono, tipo, fecha_ingreso, responsable_pago, unidad_id) VALUES
    (1, 'María José', 'Andrade Ruiz', '1712345675', 'mjandrade@correo.com', '0991234567', 'Propietario', '2022-03-15', TRUE, 1),
    (2, 'Diego', 'Paredes Luna', '1704567898', 'dparedes@correo.com', '0984561230', 'Propietario', '2021-06-01', TRUE, 2),
    (3, 'Luis', 'Andrade Mora', '1723456784', 'landrade@correo.com', '0995557788', 'Propietario', '2020-01-10', TRUE, 3),
    (4, 'Carlos', 'Mena Vaca', '1723456792', 'cmena@correo.com', '0987654321', 'Arrendatario', '2024-08-01', TRUE, 4),
    (5, 'Jorge', 'Salazar Ortiz', '1711223345', 'jsalazar@correo.com', '0991112233', 'Propietario', '2019-05-20', TRUE, 5),
    (6, 'Gabriela', 'Ortiz Ramos', '1755667787', 'gortiz@correo.com', '0982223344', 'Propietario', '2023-02-01', TRUE, 6),
    (7, 'Andrés', 'Ortiz Ramos', '1755667795', 'aortiz@correo.com', NULL, 'Propietario', '2023-02-01', FALSE, 6),
    (8, 'Sofía', 'Cárdenas Gil', '1746778891', 'scardenas@correo.com', '0976543210', 'Arrendatario', '2025-01-15', TRUE, 7),
    (9, 'Ana Lucía', 'Torres Paz', '1709876542', 'altorres@correo.com', '0961234567', 'Propietario', '2019-11-20', TRUE, 8),
    (10, 'Fernando', 'Ríos Castro', '1748990015', 'frios@correo.com', '0998887766', 'Propietario', '2018-09-01', TRUE, 9),
    (11, 'Patricia', 'Cevallos León', '1713579132', 'pcevallos@correo.com', '0993579135', 'Propietario', '2017-04-12', TRUE, 10),
    (12, 'Ricardo', 'Vega Mendoza', '1724680242', 'rvega@correo.com', '0982468024', 'Propietario', '2022-10-03', TRUE, 11),
    (13, 'Valeria', 'Mendoza Vega', '1724680259', 'vmendoza@correo.com', NULL, 'Propietario', '2022-10-03', FALSE, 11),
    (14, 'Marco', 'Benítez Haro', '1701020305', 'mbenitez@correo.com', '0990102030', 'Arrendatario', '2025-03-01', TRUE, 12),
    (15, 'Daniela', 'Suárez Páez', '1702030402', 'dsuarez@correo.com', '0990203040', 'Arrendatario', '2024-11-18', TRUE, 13);

INSERT INTO public.usuarios (id, nombre_usuario, password_hash, nombre_completo, correo, activo, creado_en, rol_id, residente_id) VALUES
    (1, 'admin', 'AQAAAAIAAYagAAAAEOu8yO///zsBLrbRfohN2bH02TeT6xMeLwsxkXGQ+5zQ4w4oS9wjsKr/qXTv9WXPNA==', 'Administrador General', 'admin@condominio360.ec', TRUE, '2026-01-02 09:00:00', 1, NULL),
    (2, 'cmena', 'AQAAAAIAAYagAAAAEFBXAXQfxWCkEuzw7+eG69ZaA0mdY99ACaQPj1q6qG3ijfiBYXa2B5Wr0InrsTsw4A==', 'Carlos Mena Vaca', 'cmena@correo.com', TRUE, '2026-01-02 09:00:00', 2, 4),
    (3, 'atorres', 'AQAAAAIAAYagAAAAEL1Wr6rcxqknTIW/sdWCOswbQ1HvwOfJzw0KlbAtzPkgtCcE9KstMnqnErcFzrfnqQ==', 'Ana Lucía Torres Paz', 'altorres@correo.com', TRUE, '2026-01-02 09:00:00', 2, 9),
    (4, 'guardia', 'AQAAAAIAAYagAAAAEABTJZ5QBEW4nnLY3rNmC2zPBfGrOIeO9ZVhjXUbO++V0KyOyFQ7UXlMJwT/mFqryA==', 'Pedro Quishpe (Garita)', 'garita@condominio360.ec', TRUE, '2026-01-02 09:00:00', 3, NULL);

INSERT INTO public.configuracion_cobro (id, dia_vencimiento, dias_gracia, tasa_recargo_mensual, vigente_desde, actualizado_por) VALUES
    (1, 10, 5, 0.0150, '2026-01-01', 1);

INSERT INTO public.cuotas (id, unidad_id, anio, mes, monto, capital_pagado, recargo_pagado, fecha_emision, fecha_vencimiento, estado, generada_por) VALUES
    (1, 1, 2026, 7, 72.00, 72.00, 0.00, '2026-07-01', '2026-07-10', 'Pagada', 1),
    (2, 2, 2026, 7, 78.00, 78.00, 0.00, '2026-07-01', '2026-07-10', 'Pagada', 1),
    (3, 3, 2026, 7, 95.00, 95.00, 0.00, '2026-07-01', '2026-07-10', 'Pagada', 1),
    (4, 4, 2026, 7, 95.00, 95.00, 0.00, '2026-07-01', '2026-07-10', 'Pagada', 1),
    (5, 5, 2026, 7, 110.00, 0.00, 0.00, '2026-07-01', '2026-07-10', 'Vencida', 1),
    (6, 6, 2026, 7, 85.00, 85.00, 2.55, '2026-07-01', '2026-07-10', 'Pagada', 1),
    (7, 7, 2026, 7, 85.00, 85.00, 0.00, '2026-07-01', '2026-07-10', 'Pagada', 1),
    (8, 8, 2026, 7, 58.50, 58.50, 0.00, '2026-07-01', '2026-07-10', 'Pagada', 1),
    (9, 9, 2026, 7, 90.00, 90.00, 0.00, '2026-07-01', '2026-07-10', 'Pagada', 1),
    (10, 10, 2026, 7, 120.00, 120.00, 0.00, '2026-07-01', '2026-07-10', 'Pagada', 1),
    (11, 11, 2026, 7, 100.00, 100.00, 0.00, '2026-07-01', '2026-07-10', 'Pagada', 1),
    (12, 12, 2026, 7, 45.00, 45.00, 0.00, '2026-07-01', '2026-07-10', 'Pagada', 1),
    (13, 13, 2026, 7, 120.00, 120.00, 0.00, '2026-07-01', '2026-07-10', 'Pagada', 1),
    (14, 1, 2026, 8, 72.00, 72.00, 0.00, '2026-08-01', '2026-08-10', 'Pagada', 1),
    (15, 2, 2026, 8, 78.00, 78.00, 0.00, '2026-08-01', '2026-08-10', 'Pagada', 1),
    (16, 3, 2026, 8, 95.00, 95.00, 0.00, '2026-08-01', '2026-08-10', 'Pagada', 1),
    (17, 4, 2026, 8, 95.00, 95.00, 0.00, '2026-08-01', '2026-08-10', 'Pagada', 1),
    (18, 5, 2026, 8, 110.00, 0.00, 0.00, '2026-08-01', '2026-08-10', 'Vencida', 1),
    (19, 6, 2026, 8, 85.00, 85.00, 1.28, '2026-08-01', '2026-08-10', 'Pagada', 1),
    (20, 7, 2026, 8, 85.00, 85.00, 0.00, '2026-08-01', '2026-08-10', 'Pagada', 1),
    (21, 8, 2026, 8, 58.50, 58.50, 0.00, '2026-08-01', '2026-08-10', 'Pagada', 1),
    (22, 9, 2026, 8, 90.00, 90.00, 0.00, '2026-08-01', '2026-08-10', 'Pagada', 1),
    (23, 10, 2026, 8, 120.00, 0.00, 0.00, '2026-08-01', '2026-08-10', 'Vencida', 1),
    (24, 11, 2026, 8, 100.00, 100.00, 0.00, '2026-08-01', '2026-08-10', 'Pagada', 1),
    (25, 12, 2026, 8, 45.00, 45.00, 0.00, '2026-08-01', '2026-08-10', 'Pagada', 1),
    (26, 13, 2026, 8, 120.00, 0.00, 0.00, '2026-08-01', '2026-08-10', 'Vencida', 1),
    (27, 1, 2026, 9, 72.00, 72.00, 0.00, '2026-09-01', '2026-09-10', 'Pagada', 1),
    (28, 2, 2026, 9, 78.00, 78.00, 0.00, '2026-09-01', '2026-09-10', 'Pagada', 1),
    (29, 3, 2026, 9, 95.00, 0.00, 0.00, '2026-09-01', '2026-09-10', 'Vencida', 1),
    (30, 4, 2026, 9, 95.00, 95.00, 0.00, '2026-09-01', '2026-09-10', 'Pagada', 1),
    (31, 5, 2026, 9, 110.00, 0.00, 0.00, '2026-09-01', '2026-09-10', 'Vencida', 1),
    (32, 6, 2026, 9, 85.00, 40.00, 0.00, '2026-09-01', '2026-09-10', 'Vencida', 1),
    (33, 7, 2026, 9, 85.00, 85.00, 0.00, '2026-09-01', '2026-09-10', 'Pagada', 1),
    (34, 8, 2026, 9, 58.50, 58.50, 0.00, '2026-09-01', '2026-09-10', 'Pagada', 1),
    (35, 9, 2026, 9, 90.00, 90.00, 0.00, '2026-09-01', '2026-09-10', 'Pagada', 1),
    (36, 10, 2026, 9, 120.00, 0.00, 0.00, '2026-09-01', '2026-09-10', 'Vencida', 1),
    (37, 11, 2026, 9, 100.00, 100.00, 0.00, '2026-09-01', '2026-09-10', 'Pagada', 1),
    (38, 12, 2026, 9, 45.00, 45.00, 0.00, '2026-09-01', '2026-09-10', 'Pagada', 1),
    (39, 13, 2026, 9, 120.00, 0.00, 0.00, '2026-09-01', '2026-09-10', 'Vencida', 1),
    (40, 1, 2026, 10, 72.00, 72.00, 0.00, '2026-10-01', '2026-10-10', 'Pagada', 1),
    (41, 2, 2026, 10, 78.00, 78.00, 0.00, '2026-10-01', '2026-10-10', 'Pagada', 1),
    (42, 3, 2026, 10, 95.00, 0.00, 0.00, '2026-10-01', '2026-10-10', 'Vencida', 1),
    (43, 4, 2026, 10, 95.00, 0.00, 0.00, '2026-10-01', '2026-10-10', 'Vencida', 1),
    (44, 5, 2026, 10, 110.00, 0.00, 0.00, '2026-10-01', '2026-10-10', 'Vencida', 1),
    (45, 6, 2026, 10, 85.00, 0.00, 0.00, '2026-10-01', '2026-10-10', 'Vencida', 1),
    (46, 7, 2026, 10, 85.00, 85.00, 0.00, '2026-10-01', '2026-10-10', 'Pagada', 1),
    (47, 8, 2026, 10, 58.50, 0.00, 0.00, '2026-10-01', '2026-10-10', 'Vencida', 1),
    (48, 9, 2026, 10, 90.00, 90.00, 0.00, '2026-10-01', '2026-10-10', 'Pagada', 1),
    (49, 10, 2026, 10, 120.00, 0.00, 0.00, '2026-10-01', '2026-10-10', 'Vencida', 1),
    (50, 11, 2026, 10, 100.00, 100.00, 0.00, '2026-10-01', '2026-10-10', 'Pagada', 1),
    (51, 12, 2026, 10, 45.00, 0.00, 0.00, '2026-10-01', '2026-10-10', 'Vencida', 1),
    (52, 13, 2026, 10, 120.00, 0.00, 0.00, '2026-10-01', '2026-10-10', 'Vencida', 1);

INSERT INTO public.pagos (id, unidad_id, residente_id, registrado_por, verificado_por, fecha_pago, monto, metodo, referencia, comprobante, estado, fecha_verificacion, observacion) VALUES
    (1, 1, 1, 1, 1, '2026-07-08', 72.00, 'Transferencia', 'TRX-010708', 'comprobantes/pago-001.jpg', 'Verificado', '2026-07-09 10:00:00', NULL),
    (2, 1, 1, 1, 1, '2026-08-08', 72.00, 'Transferencia', 'TRX-010808', 'comprobantes/pago-002.jpg', 'Verificado', '2026-08-09 10:00:00', NULL),
    (3, 1, 1, 1, 1, '2026-09-08', 72.00, 'Transferencia', 'TRX-010908', 'comprobantes/pago-003.jpg', 'Verificado', '2026-09-09 10:00:00', NULL),
    (4, 1, 1, 1, 1, '2026-10-08', 72.00, 'Transferencia', 'TRX-011008', 'comprobantes/pago-004.jpg', 'Verificado', '2026-10-09 10:00:00', NULL),
    (5, 2, 2, 1, 1, '2026-07-08', 78.00, 'Deposito', 'DEP-020708', 'comprobantes/pago-005.jpg', 'Verificado', '2026-07-09 10:00:00', NULL),
    (6, 2, 2, 1, 1, '2026-08-08', 78.00, 'Deposito', 'DEP-020808', 'comprobantes/pago-006.jpg', 'Verificado', '2026-08-09 10:00:00', NULL),
    (7, 2, 2, 1, 1, '2026-09-08', 78.00, 'Deposito', 'DEP-020908', 'comprobantes/pago-007.jpg', 'Verificado', '2026-09-09 10:00:00', NULL),
    (8, 2, 2, 1, 1, '2026-10-08', 78.00, 'Deposito', 'DEP-021008', 'comprobantes/pago-008.jpg', 'Verificado', '2026-10-09 10:00:00', NULL),
    (9, 3, 3, 1, 1, '2026-07-08', 95.00, 'Transferencia', 'TRX-030708', 'comprobantes/pago-009.jpg', 'Verificado', '2026-07-09 10:00:00', NULL),
    (10, 3, 3, 1, 1, '2026-08-08', 95.00, 'Transferencia', 'TRX-030808', 'comprobantes/pago-010.jpg', 'Verificado', '2026-08-09 10:00:00', NULL),
    (11, 3, 3, 1, 1, '2026-09-11', 95.00, 'Deposito', 'DEP-00931', 'comprobantes/pago-011.jpg', 'Rechazado', '2026-09-12 10:00:00', 'Comprobante ilegible; el banco no registra el depósito.'),
    (12, 4, 4, 1, 1, '2026-07-08', 95.00, 'Transferencia', 'TRX-040708', 'comprobantes/pago-012.jpg', 'Verificado', '2026-07-09 10:00:00', NULL),
    (13, 4, 4, 1, 1, '2026-08-08', 95.00, 'Transferencia', 'TRX-040808', 'comprobantes/pago-013.jpg', 'Verificado', '2026-08-09 10:00:00', NULL),
    (14, 4, 4, 1, 1, '2026-09-08', 95.00, 'Transferencia', 'TRX-040908', 'comprobantes/pago-014.jpg', 'Verificado', '2026-09-09 10:00:00', NULL),
    (15, 6, 6, 1, 1, '2026-08-20', 173.83, 'Transferencia', 'TRX-558120', 'comprobantes/pago-015.jpg', 'Verificado', '2026-08-21 10:00:00', NULL),
    (16, 6, 6, 1, 1, '2026-09-12', 40.00, 'Efectivo', NULL, NULL, 'Verificado', '2026-09-13 10:00:00', NULL),
    (17, 7, 8, 1, 1, '2026-07-08', 85.00, 'Efectivo', NULL, NULL, 'Verificado', '2026-07-09 10:00:00', NULL),
    (18, 7, 8, 1, 1, '2026-08-08', 85.00, 'Efectivo', NULL, NULL, 'Verificado', '2026-08-09 10:00:00', NULL),
    (19, 7, 8, 1, 1, '2026-09-08', 85.00, 'Efectivo', NULL, NULL, 'Verificado', '2026-09-09 10:00:00', NULL),
    (20, 7, 8, 1, 1, '2026-10-08', 85.00, 'Efectivo', NULL, NULL, 'Verificado', '2026-10-09 10:00:00', NULL),
    (21, 8, 9, 1, 1, '2026-07-08', 58.50, 'Transferencia', 'TRX-080708', 'comprobantes/pago-021.jpg', 'Verificado', '2026-07-09 10:00:00', NULL),
    (22, 8, 9, 1, 1, '2026-08-08', 58.50, 'Transferencia', 'TRX-080808', 'comprobantes/pago-022.jpg', 'Verificado', '2026-08-09 10:00:00', NULL),
    (23, 8, 9, 1, 1, '2026-09-08', 58.50, 'Transferencia', 'TRX-080908', 'comprobantes/pago-023.jpg', 'Verificado', '2026-09-09 10:00:00', NULL),
    (24, 9, 10, 1, 1, '2026-07-08', 90.00, 'Transferencia', 'TRX-090708', 'comprobantes/pago-024.jpg', 'Verificado', '2026-07-09 10:00:00', NULL),
    (25, 9, 10, 1, 1, '2026-08-08', 90.00, 'Transferencia', 'TRX-090808', 'comprobantes/pago-025.jpg', 'Verificado', '2026-08-09 10:00:00', NULL),
    (26, 9, 10, 1, 1, '2026-09-08', 90.00, 'Transferencia', 'TRX-090908', 'comprobantes/pago-026.jpg', 'Verificado', '2026-09-09 10:00:00', NULL),
    (27, 9, 10, 1, 1, '2026-10-08', 90.00, 'Transferencia', 'TRX-091008', 'comprobantes/pago-027.jpg', 'Verificado', '2026-10-09 10:00:00', NULL),
    (28, 10, 11, 1, 1, '2026-07-08', 120.00, 'Deposito', 'DEP-100708', 'comprobantes/pago-028.jpg', 'Verificado', '2026-07-09 10:00:00', NULL),
    (29, 11, 12, 1, 1, '2026-07-08', 100.00, 'Transferencia', 'TRX-110708', 'comprobantes/pago-029.jpg', 'Verificado', '2026-07-09 10:00:00', NULL),
    (30, 11, 12, 1, 1, '2026-08-08', 100.00, 'Transferencia', 'TRX-110808', 'comprobantes/pago-030.jpg', 'Verificado', '2026-08-09 10:00:00', NULL),
    (31, 11, 12, 1, 1, '2026-09-09', 100.00, 'Transferencia', 'TRX-110909', 'comprobantes/pago-031.jpg', 'Verificado', '2026-09-10 10:00:00', NULL),
    (32, 11, 12, 1, 1, '2026-10-03', 100.00, 'Transferencia', 'TRX-111003', 'comprobantes/pago-032.jpg', 'Verificado', '2026-10-04 10:00:00', NULL),
    (33, 12, 14, 1, 1, '2026-07-08', 45.00, 'Efectivo', NULL, NULL, 'Verificado', '2026-07-09 10:00:00', NULL),
    (34, 12, 14, 1, 1, '2026-08-08', 45.00, 'Efectivo', NULL, NULL, 'Verificado', '2026-08-09 10:00:00', NULL),
    (35, 12, 14, 1, 1, '2026-09-08', 45.00, 'Efectivo', NULL, NULL, 'Verificado', '2026-09-09 10:00:00', NULL),
    (36, 13, 15, 1, 1, '2026-07-08', 120.00, 'Deposito', 'DEP-130708', 'comprobantes/pago-036.jpg', 'Verificado', '2026-07-09 10:00:00', NULL),
    (37, 4, 4, 2, NULL, '2026-10-25', 96.43, 'Transferencia', 'TRX-771045', 'comprobantes/pago-037.jpg', 'PorVerificar', NULL, NULL),
    (38, 8, 9, 3, NULL, '2026-10-26', 30.00, 'Deposito', 'DEP-20451', 'comprobantes/pago-038.jpg', 'PorVerificar', NULL, NULL);

INSERT INTO public.aplicaciones_pago (id, pago_id, cuota_id, monto_recargo, monto_capital) VALUES
    (1, 1, 1, 0, 72.00),
    (2, 2, 14, 0, 72.00),
    (3, 3, 27, 0, 72.00),
    (4, 4, 40, 0, 72.00),
    (5, 5, 2, 0, 78.00),
    (6, 6, 15, 0, 78.00),
    (7, 7, 28, 0, 78.00),
    (8, 8, 41, 0, 78.00),
    (9, 9, 3, 0, 95.00),
    (10, 10, 16, 0, 95.00),
    (11, 12, 4, 0, 95.00),
    (12, 13, 17, 0, 95.00),
    (13, 14, 30, 0, 95.00),
    (14, 15, 6, 2.55, 85.00),
    (15, 15, 19, 1.28, 85.00),
    (16, 16, 32, 0, 40.00),
    (17, 17, 7, 0, 85.00),
    (18, 18, 20, 0, 85.00),
    (19, 19, 33, 0, 85.00),
    (20, 20, 46, 0, 85.00),
    (21, 21, 8, 0, 58.50),
    (22, 22, 21, 0, 58.50),
    (23, 23, 34, 0, 58.50),
    (24, 24, 9, 0, 90.00),
    (25, 25, 22, 0, 90.00),
    (26, 26, 35, 0, 90.00),
    (27, 27, 48, 0, 90.00),
    (28, 28, 10, 0, 120.00),
    (29, 29, 11, 0, 100.00),
    (30, 30, 24, 0, 100.00),
    (31, 31, 37, 0, 100.00),
    (32, 32, 50, 0, 100.00),
    (33, 33, 12, 0, 45.00),
    (34, 34, 25, 0, 45.00),
    (35, 35, 38, 0, 45.00),
    (36, 36, 13, 0, 120.00);

INSERT INTO public.areas_comunales (id, nombre, descripcion, capacidad, hora_apertura, hora_cierre, costo_uso, activa) VALUES
    (1, 'Salón comunal', 'Salón para eventos con cocina y baños', 50, '08:00', '22:00', 25.00, TRUE),
    (2, 'Zona BBQ', 'Parrilla cubierta junto al jardín', 12, '10:00', '21:00', 10.00, TRUE),
    (3, 'Gimnasio', 'Máquinas de cardio y pesas', 10, '06:00', '22:00', 0.00, TRUE);

INSERT INTO public.reservas (id, area_id, residente_id, fecha, hora_inicio, hora_fin, estado, aprobada_por) VALUES
    (1, 1, 1, '2026-11-07', '14:00', '20:00', 'Aprobada', 1),
    (2, 2, 9, '2026-11-01', '12:00', '16:00', 'Solicitada', NULL);

INSERT INTO public.visitas (id, unidad_id, autorizada_por, registrada_por, nombre_visitante, cedula_visitante, placa_vehiculo, fecha_esperada, hora_ingreso, hora_salida, estado) VALUES
    (1, 4, 4, 4, 'Lucía Mena', '1718181819', 'PBX-1234', '2026-10-20', '2026-10-20 18:05:00', '2026-10-20 21:40:00', 'Finalizada'),
    (2, 8, 9, NULL, 'Técnico de internet', '1705050506', NULL, '2026-10-27', NULL, NULL, 'Autorizada'),
    (3, 1, 1, NULL, 'Esteban Andrade', '1720202025', 'PCA-5678', '2026-10-27', NULL, NULL, 'Autorizada');

-- Ajusta las secuencias identity después de insertar con id explícito
SELECT setval(pg_get_serial_sequence('public.roles', 'id'), (SELECT max(id) FROM public.roles));
SELECT setval(pg_get_serial_sequence('public.unidades', 'id'), (SELECT max(id) FROM public.unidades));
SELECT setval(pg_get_serial_sequence('public.residentes', 'id'), (SELECT max(id) FROM public.residentes));
SELECT setval(pg_get_serial_sequence('public.usuarios', 'id'), (SELECT max(id) FROM public.usuarios));
SELECT setval(pg_get_serial_sequence('public.configuracion_cobro', 'id'), (SELECT max(id) FROM public.configuracion_cobro));
SELECT setval(pg_get_serial_sequence('public.cuotas', 'id'), (SELECT max(id) FROM public.cuotas));
SELECT setval(pg_get_serial_sequence('public.pagos', 'id'), (SELECT max(id) FROM public.pagos));
SELECT setval(pg_get_serial_sequence('public.aplicaciones_pago', 'id'), (SELECT max(id) FROM public.aplicaciones_pago));
SELECT setval(pg_get_serial_sequence('public.areas_comunales', 'id'), (SELECT max(id) FROM public.areas_comunales));
SELECT setval(pg_get_serial_sequence('public.reservas', 'id'), (SELECT max(id) FROM public.reservas));
SELECT setval(pg_get_serial_sequence('public.visitas', 'id'), (SELECT max(id) FROM public.visitas));
