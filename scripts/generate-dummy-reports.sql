-- Script para generar 20 reportes dummy del día 08 de diciembre de 2025
-- Para propósitos de prueba del sistema de reportes automatizados con IA

-- Nota: Ajustar el usuario_id según el usuario CREADOR en su base de datos
DO $$
DECLARE
    usuario_creador UUID := '019ac648-b096-7afc-b2cb-6610f7b6711f'; -- creador@test.com
    fecha_base TIMESTAMP := '2025-12-08 00:00:00'::timestamp AT TIME ZONE 'UTC';

BEGIN
    -- Reporte 1: Violencia familiar, Zona Norte
    INSERT INTO "REPORTE_INCIDENCIA" (
        tipo_reporte, estado, datetime_hechos, sexo, edad,
        lgbtttiq_plus, situacion_calle, migrante, discapacidad,
        delito, zona_id, sector_id, cuadrante_id, turno_ceiba,
        tipo_de_atencion, tipo_de_accion, hechos_reportados, acciones_realizadas,
        traslados, observaciones, usuario_id, created_at
    ) VALUES (
        'A', 1, fecha_base + INTERVAL '8 hours', 'Femenino', 32,
        false, false, false, false,
        'Violencia familiar', 1, 1, 1, 1,
        'Presencial', 1,
        'Se recibió llamada de vecinos reportando gritos y ruidos violentos. Al llegar al domicilio, la víctima presentaba hematomas en brazo izquierdo. Relata agresión física por parte de su pareja.',
        'Se brindó atención psicológica de primera instancia. Se canalizó a la víctima con el área de trabajo social para seguimiento. Se elaboró acta circunstanciada del evento.',
        1, 'Víctima fue trasladada al refugio temporal',
        usuario_creador, fecha_base + INTERVAL '8 hours'
    );

    -- Reporte 2: Acoso sexual, Zona Sur
    INSERT INTO "REPORTE_INCIDENCIA" (
        tipo_reporte, estado, datetime_hechos, sexo, edad,
        lgbtttiq_plus, situacion_calle, migrante, discapacidad,
        delito, zona_id, sector_id, cuadrante_id, turno_ceiba,
        tipo_de_atencion, tipo_de_accion, hechos_reportados, acciones_realizadas,
        traslados, usuario_id, created_at
    ) VALUES (
        'A', 1, fecha_base + INTERVAL '10 hours', 'Femenino', 24,
        false, false, false, false,
        'Acoso sexual', 2, 4, 13, 1,
        'Presencial', 1,
        'Mujer reporta acoso sexual constante en su lugar de trabajo. Describe comentarios inapropiados y acercamientos no consentidos por parte de su supervisor durante las últimas dos semanas.',
        'Se tomó declaración detallada de los hechos. Se orientó sobre proceso legal y derechos laborales. Se proporcionaron contactos de instituciones especializadas en acoso laboral.',
        0,
        usuario_creador, fecha_base + INTERVAL '10 hours'
    );

    -- Reporte 3: Violación, Zona Centro
    INSERT INTO "REPORTE_INCIDENCIA" (
        tipo_reporte, estado, datetime_hechos, sexo, edad,
        lgbtttiq_plus, situacion_calle, migrante, discapacidad,
        delito, zona_id, sector_id, cuadrante_id, turno_ceiba,
        tipo_de_atencion, tipo_de_accion, hechos_reportados, acciones_realizadas,
        traslados, observaciones, usuario_id, created_at
    ) VALUES (
        'A', 1, fecha_base + INTERVAL '2 hours', 'Femenino', 19,
        false, false, false, false,
        'Violación', 3, 8, 28, 2,
        'Presencial', 1,
        'Víctima reporta agresión sexual ocurrida en horas de la madrugada. Presenta estado de shock emocional severo. Solicita atención médica urgente.',
        'Se activó protocolo de atención a víctimas de violencia sexual. Se trasladó inmediatamente a hospital para atención médica y recolección de evidencias. Se notificó a Ministerio Público.',
        1, 'Caso de alta prioridad, requiere seguimiento psicológico',
        usuario_creador, fecha_base + INTERVAL '2 hours'
    );

    -- Reporte 4: Lesiones dolosas, Zona Norte
    INSERT INTO "REPORTE_INCIDENCIA" (
        tipo_reporte, estado, datetime_hechos, sexo, edad,
        lgbtttiq_plus, situacion_calle, migrante, discapacidad,
        delito, zona_id, sector_id, cuadrante_id, turno_ceiba,
        tipo_de_atencion, tipo_de_accion, hechos_reportados, acciones_realizadas,
        traslados, usuario_id, created_at
    ) VALUES (
        'A', 1, fecha_base + INTERVAL '14 hours', 'Masculino', 28,
        true, false, false, false,
        'Lesiones', 1, 2, 4, 1,
        'Presencial', 1,
        'Persona de la comunidad LGBTTTIQ+ reporta agresión física motivada por discriminación. Presenta lesiones menores en rostro. El agresor profirió insultos homofóbicos.',
        'Se documentaron las lesiones con fotografías. Se brindó atención médica básica. Se orientó sobre denuncia por delito de odio. Se ofreció acompañamiento legal.',
        0,
        usuario_creador, fecha_base + INTERVAL '14 hours'
    );

    -- Reporte 5: Amenazas, Zona Sur
    INSERT INTO "REPORTE_INCIDENCIA" (
        tipo_reporte, estado, datetime_hechos, sexo, edad,
        lgbtttiq_plus, situacion_calle, migrante, discapacidad,
        delito, zona_id, sector_id, cuadrante_id, turno_ceiba,
        tipo_de_atencion, tipo_de_accion, hechos_reportados, acciones_realizadas,
        traslados, usuario_id, created_at
    ) VALUES (
        'A', 1, fecha_base + INTERVAL '16 hours', 'Femenino', 45,
        false, false, false, false,
        'Amenazas', 2, 5, 16, 2,
        'Presencial', 1,
        'Mujer reporta amenazas constantes vía telefónica y mensajes de texto por parte de ex pareja. Teme por su seguridad y la de sus hijos menores.',
        'Se registraron evidencias digitales de las amenazas. Se orientó sobre medidas de protección y órdenes de alejamiento. Se elaboró plan de seguridad personalizado.',
        0,
        usuario_creador, fecha_base + INTERVAL '16 hours'
    );

    -- Reporte 6: Violencia familiar, Zona Norte
    INSERT INTO "REPORTE_INCIDENCIA" (
        tipo_reporte, estado, datetime_hechos, sexo, edad,
        lgbtttiq_plus, situacion_calle, migrante, discapacidad,
        delito, zona_id, sector_id, cuadrante_id, turno_ceiba,
        tipo_de_atencion, tipo_de_accion, hechos_reportados, acciones_realizadas,
        traslados, usuario_id, created_at
    ) VALUES (
        'A', 1, fecha_base + INTERVAL '11 hours', 'Femenino', 38,
        false, false, true, false,
        'Violencia familiar', 1, 3, 8, 1,
        'Presencial', 1,
        'Mujer migrante reporta violencia económica y psicológica por parte de su esposo. Relata control total de sus ingresos y aislamiento de su familia.',
        'Se proporcionó información sobre derechos de personas migrantes. Se ofreció asesoría legal migratoria. Se contactó con organizaciones de apoyo a migrantes.',
        0,
        usuario_creador, fecha_base + INTERVAL '11 hours'
    );

    -- Reporte 7: Hostigamiento sexual, Zona Centro
    INSERT INTO "REPORTE_INCIDENCIA" (
        tipo_reporte, estado, datetime_hechos, sexo, edad,
        lgbtttiq_plus, situacion_calle, migrante, discapacidad,
        delito, zona_id, sector_id, cuadrante_id, turno_ceiba,
        tipo_de_atencion, tipo_de_accion, hechos_reportados, acciones_realizadas,
        traslados, usuario_id, created_at
    ) VALUES (
        'A', 1, fecha_base + INTERVAL '9 hours', 'Femenino', 27,
        false, false, false, false,
        'Hostigamiento sexual', 3, 8, 29, 1,
        'Presencial', 1,
        'Estudiante universitaria reporta hostigamiento sexual por parte de profesor. Describe proposiciones indebidas condicionadas a calificaciones.',
        'Se documentó el caso con evidencias proporcionadas. Se orientó sobre procedimiento institucional y legal. Se proporcionó contacto de defensoría universitaria.',
        0,
        usuario_creador, fecha_base + INTERVAL '9 hours'
    );

    -- Reporte 8: Violencia en el noviazgo, Zona Sur
    INSERT INTO "REPORTE_INCIDENCIA" (
        tipo_reporte, estado, datetime_hechos, sexo, edad,
        lgbtttiq_plus, situacion_calle, migrante, discapacidad,
        delito, zona_id, sector_id, cuadrante_id, turno_ceiba,
        tipo_de_atencion, tipo_de_accion, hechos_reportados, acciones_realizadas,
        traslados, usuario_id, created_at
    ) VALUES (
        'A', 1, fecha_base + INTERVAL '15 hours', 'Femenino', 21,
        false, false, false, false,
        'Violencia en el noviazgo', 2, 6, 20, 2,
        'Presencial', 3,
        'Joven reporta violencia psicológica y control excesivo por parte de su novio. Describe celos patológicos, revisión de celular, y aislamiento de amistades.',
        'Se impartió sesión de prevención sobre relaciones sanas. Se proporcionó material informativo sobre señales de violencia en el noviazgo. Se ofreció seguimiento.',
        0,
        usuario_creador, fecha_base + INTERVAL '15 hours'
    );

    -- Reporte 9: Abuso sexual infantil, Zona Norte
    INSERT INTO "REPORTE_INCIDENCIA" (
        tipo_reporte, estado, datetime_hechos, sexo, edad,
        lgbtttiq_plus, situacion_calle, migrante, discapacidad,
        delito, zona_id, sector_id, cuadrante_id, turno_ceiba,
        tipo_de_atencion, tipo_de_accion, hechos_reportados, acciones_realizadas,
        traslados, observaciones, usuario_id, created_at
    ) VALUES (
        'A', 1, fecha_base + INTERVAL '13 hours', 'Femenino', 12,
        false, false, false, false,
        'Abuso sexual infantil', 1, 1, 2, 1,
        'Presencial', 1,
        'Madre reporta sospecha de abuso sexual contra menor de edad. Describe cambios de comportamiento súbitos y relato de la menor sobre tocamientos inapropiados.',
        'Se activó protocolo de protección a menores. Se notificó a DIF y Ministerio Público. Se canalizó a psicología forense. Se separó al presunto agresor del hogar.',
        1, 'Caso grave, requiere intervención inmediata de autoridades',
        usuario_creador, fecha_base + INTERVAL '13 hours'
    );

    -- Reporte 10: Discriminación, Zona Centro
    INSERT INTO "REPORTE_INCIDENCIA" (
        tipo_reporte, estado, datetime_hechos, sexo, edad,
        lgbtttiq_plus, situacion_calle, migrante, discapacidad,
        delito, zona_id, sector_id, cuadrante_id, turno_ceiba,
        tipo_de_atencion, tipo_de_accion, hechos_reportados, acciones_realizadas,
        traslados, usuario_id, created_at
    ) VALUES (
        'A', 1, fecha_base + INTERVAL '12 hours', 'No binario', 29,
        true, false, false, false,
        'Discriminación', 3, 8, 30, 1,
        'Presencial', 1,
        'Persona no binaria reporta discriminación en establecimiento comercial. Se le negó el servicio y se le pidió retirarse basándose en su expresión de género.',
        'Se documentó el incidente con testigos. Se orientó sobre recursos legales contra discriminación. Se proporcionó contacto de CONAPRED.',
        0,
        usuario_creador, fecha_base + INTERVAL '12 hours'
    );

    -- Reporte 11: Violencia digital, Zona Sur
    INSERT INTO "REPORTE_INCIDENCIA" (
        tipo_reporte, estado, datetime_hechos, sexo, edad,
        lgbtttiq_plus, situacion_calle, migrante, discapacidad,
        delito, zona_id, sector_id, cuadrante_id, turno_ceiba,
        tipo_de_atencion, tipo_de_accion, hechos_reportados, acciones_realizadas,
        traslados, usuario_id, created_at
    ) VALUES (
        'A', 1, fecha_base + INTERVAL '17 hours', 'Femenino', 23,
        false, false, false, false,
        'Violencia digital', 2, 7, 25, 2,
        'Presencial', 1,
        'Joven reporta difusión no autorizada de imágenes íntimas por parte de ex pareja. Las fotografías están siendo compartidas en redes sociales.',
        'Se documentaron las evidencias digitales. Se orientó sobre Ley Olimpia. Se apoyó en proceso de denuncia ante ciberpolicía. Se brindó contención emocional.',
        0,
        usuario_creador, fecha_base + INTERVAL '17 hours'
    );

    -- Reporte 12: Violencia obstétrica, Zona Norte
    INSERT INTO "REPORTE_INCIDENCIA" (
        tipo_reporte, estado, datetime_hechos, sexo, edad,
        lgbtttiq_plus, situacion_calle, migrante, discapacidad,
        delito, zona_id, sector_id, cuadrante_id, turno_ceiba,
        tipo_de_atencion, tipo_de_accion, hechos_reportados, acciones_realizadas,
        traslados, usuario_id, created_at
    ) VALUES (
        'A', 1, fecha_base + INTERVAL '10.5 hours', 'Femenino', 26,
        false, false, false, false,
        'Violencia obstétrica', 1, 2, 5, 1,
        'Presencial', 1,
        'Mujer reporta maltrato durante atención de parto. Describe comentarios degradantes, procedimientos sin consentimiento, y negación de acompañamiento.',
        'Se registraron detalles del caso. Se orientó sobre derechos reproductivos. Se canalizó con CONAMED para presentar queja formal contra institución médica.',
        0,
        usuario_creador, fecha_base + INTERVAL '10.5 hours'
    );

    -- Reporte 13: Trata de personas, Zona Centro
    INSERT INTO "REPORTE_INCIDENCIA" (
        tipo_reporte, estado, datetime_hechos, sexo, edad,
        lgbtttiq_plus, situacion_calle, migrante, discapacidad,
        delito, zona_id, sector_id, cuadrante_id, turno_ceiba,
        tipo_de_atencion, tipo_de_accion, hechos_reportados, acciones_realizadas,
        traslados, observaciones, usuario_id, created_at
    ) VALUES (
        'A', 1, fecha_base + INTERVAL '3 hours', 'Femenino', 17,
        false, false, true, false,
        'Trata de personas', 3, 8, 28, 2,
        'Presencial', 1,
        'Menor migrante logró escapar de red de trata. Fue engañada con promesa de empleo y forzada a ejercer prostitución. Presenta signos de maltrato físico.',
        'Se activó protocolo de rescate de víctimas de trata. Se trasladó a refugio especializado. Se notificó a fiscalía especializada y consulado. Requiere protección inmediata.',
        1, 'Caso federal de alta complejidad, coordinación interinstitucional activa',
        usuario_creador, fecha_base + INTERVAL '3 hours'
    );

    -- Reporte 14: Violencia patrimonial, Zona Sur
    INSERT INTO "REPORTE_INCIDENCIA" (
        tipo_reporte, estado, datetime_hechos, sexo, edad,
        lgbtttiq_plus, situacion_calle, migrante, discapacidad,
        delito, zona_id, sector_id, cuadrante_id, turno_ceiba,
        tipo_de_atencion, tipo_de_accion, hechos_reportados, acciones_realizadas,
        traslados, usuario_id, created_at
    ) VALUES (
        'A', 1, fecha_base + INTERVAL '14.5 hours', 'Femenino', 52,
        false, false, false, true,
        'Violencia patrimonial', 2, 5, 17, 2,
        'Presencial', 1,
        'Mujer adulta mayor con discapacidad reporta despojo de su vivienda por parte de familiares. Fue forzada a firmar documentos sin comprender su contenido.',
        'Se documentó el caso. Se contactó con defensoría pública para asesoría legal. Se coordinó con DIF para evaluación de capacidad jurídica. Se ofreció refugio temporal.',
        0,
        usuario_creador, fecha_base + INTERVAL '14.5 hours'
    );

    -- Reporte 15: Acoso callejero, Zona Norte
    INSERT INTO "REPORTE_INCIDENCIA" (
        tipo_reporte, estado, datetime_hechos, sexo, edad,
        lgbtttiq_plus, situacion_calle, migrante, discapacidad,
        delito, zona_id, sector_id, cuadrante_id, turno_ceiba,
        tipo_de_atencion, tipo_de_accion, hechos_reportados, acciones_realizadas,
        traslados, usuario_id, created_at
    ) VALUES (
        'A', 1, fecha_base + INTERVAL '18 hours', 'Femenino', 19,
        false, false, false, false,
        'Acoso callejero', 1, 3, 9, 2,
        'Presencial', 3,
        'Estudiante reporta acoso constante en su ruta de transporte público. Describe seguimientos, comentarios sexuales, y acercamientos invasivos.',
        'Se realizó sesión de prevención sobre acoso en espacios públicos. Se proporcionaron números de emergencia. Se organizó operativo preventivo en la zona reportada.',
        0,
        usuario_creador, fecha_base + INTERVAL '18 hours'
    );

    -- Reporte 16: Violencia económica, Zona Centro
    INSERT INTO "REPORTE_INCIDENCIA" (
        tipo_reporte, estado, datetime_hechos, sexo, edad,
        lgbtttiq_plus, situacion_calle, migrante, discapacidad,
        delito, zona_id, sector_id, cuadrante_id, turno_ceiba,
        tipo_de_atencion, tipo_de_accion, hechos_reportados, acciones_realizadas,
        traslados, usuario_id, created_at
    ) VALUES (
        'A', 1, fecha_base + INTERVAL '11.5 hours', 'Femenino', 41,
        false, false, false, false,
        'Violencia económica', 3, 8, 29, 1,
        'Presencial', 1,
        'Mujer reporta que su esposo le impide trabajar y controla todos los recursos económicos del hogar. No tiene acceso a dinero para necesidades básicas.',
        'Se brindó orientación sobre violencia económica y derechos. Se proporcionó información de programas de empoderamiento económico. Se ofreció asesoría legal.',
        0,
        usuario_creador, fecha_base + INTERVAL '11.5 hours'
    );

    -- Reporte 17: Feminicidio en grado de tentativa, Zona Sur
    INSERT INTO "REPORTE_INCIDENCIA" (
        tipo_reporte, estado, datetime_hechos, sexo, edad,
        lgbtttiq_plus, situacion_calle, migrante, discapacidad,
        delito, zona_id, sector_id, cuadrante_id, turno_ceiba,
        tipo_de_atencion, tipo_de_accion, hechos_reportados, acciones_realizadas,
        traslados, observaciones, usuario_id, created_at
    ) VALUES (
        'A', 1, fecha_base + INTERVAL '1 hour', 'Femenino', 34,
        false, true, false, false,
        'Tentativa de feminicidio', 2, 4, 14, 2,
        'Presencial', 1,
        'Mujer en situación de calle fue atacada con arma blanca por desconocido. Presenta múltiples heridas defensivas. El agresor intentó estrangularla.',
        'Se activó código rojo. Traslado urgente a hospital. Se resguardaron evidencias. Se notificó a fiscalía de feminicidios. Se inició protocolo Alba.',
        1, 'Emergencia médica, riesgo de vida, custodia policial activa',
        usuario_creador, fecha_base + INTERVAL '1 hour'
    );

    -- Reporte 18: Violencia laboral, Zona Norte
    INSERT INTO "REPORTE_INCIDENCIA" (
        tipo_reporte, estado, datetime_hechos, sexo, edad,
        lgbtttiq_plus, situacion_calle, migrante, discapacidad,
        delito, zona_id, sector_id, cuadrante_id, turno_ceiba,
        tipo_de_atencion, tipo_de_accion, hechos_reportados, acciones_realizadas,
        traslados, usuario_id, created_at
    ) VALUES (
        'A', 1, fecha_base + INTERVAL '16.5 hours', 'Femenino', 36,
        false, false, false, false,
        'Violencia laboral', 1, 1, 3, 2,
        'Presencial', 1,
        'Trabajadora reporta despido injustificado tras solicitar licencia por embarazo. Describe discriminación sistemática y comentarios peyorativos sobre maternidad.',
        'Se documentó el caso laboral. Se orientó sobre protección de maternidad en LFT. Se proporcionó contacto de PROFEDET. Se ofreció acompañamiento en proceso.',
        0,
        usuario_creador, fecha_base + INTERVAL '16.5 hours'
    );

    -- Reporte 19: Violencia política, Zona Centro
    INSERT INTO "REPORTE_INCIDENCIA" (
        tipo_reporte, estado, datetime_hechos, sexo, edad,
        lgbtttiq_plus, situacion_calle, migrante, discapacidad,
        delito, zona_id, sector_id, cuadrante_id, turno_ceiba,
        tipo_de_atencion, tipo_de_accion, hechos_reportados, acciones_realizadas,
        traslados, usuario_id, created_at
    ) VALUES (
        'A', 1, fecha_base + INTERVAL '13.5 hours', 'Femenino', 47,
        false, false, false, false,
        'Violencia política de género', 3, 8, 30, 1,
        'Presencial', 1,
        'Regidora municipal reporta amenazas y obstrucción de funciones por parte de colegas varones. Describen actos sistemáticos para impedir su participación política.',
        'Se registraron evidencias. Se orientó sobre Ley de Violencia Política contra las Mujeres. Se canalizó con FEPADE. Se coordinó con instituto electoral local.',
        0,
        usuario_creador, fecha_base + INTERVAL '13.5 hours'
    );

    -- Reporte 20: Violencia psicológica, Zona Sur
    INSERT INTO "REPORTE_INCIDENCIA" (
        tipo_reporte, estado, datetime_hechos, sexo, edad,
        lgbtttiq_plus, situacion_calle, migrante, discapacidad,
        delito, zona_id, sector_id, cuadrante_id, turno_ceiba,
        tipo_de_atencion, tipo_de_accion, hechos_reportados, acciones_realizadas,
        traslados, usuario_id, created_at
    ) VALUES (
        'A', 1, fecha_base + INTERVAL '19 hours', 'Femenino', 30,
        false, false, false, false,
        'Violencia psicológica', 2, 6, 21, 2,
        'Presencial', 2,
        'Mujer reporta violencia psicológica prolongada en relación de pareja. Describe manipulación, humillaciones constantes, y gaslighting que afectan su salud mental.',
        'Se realizó capacitación sobre violencia psicológica y sus efectos. Se proporcionó atención psicológica. Se elaboró plan de seguridad. Se ofreció terapia continua.',
        0,
        usuario_creador, fecha_base + INTERVAL '19 hours'
    );

    RAISE NOTICE '20 reportes dummy generados exitosamente para el día 08/12/2025';

END $$;
