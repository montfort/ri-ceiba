# Configuración de Mailgun para Ceiba

## Error "Forbidden" - Causas y Soluciones

El error `403 Forbidden` al enviar emails con Mailgun generalmente se debe a:

### 1. Uso de Dominio Sandbox sin Autorización

Mailgun proporciona un dominio sandbox para pruebas (ej: `sandboxXXXXXXX.mailgun.org`), pero **SOLO permite enviar emails a direcciones autorizadas**.

#### Solución:

1. **Ir al Dashboard de Mailgun**: https://app.mailgun.com/
2. **Navegar a**: Sending → Domain Settings → Authorized Recipients
3. **Agregar el email de prueba**: Agregar la dirección a la que deseas enviar
4. **Confirmar el email**: Mailgun enviará un email de confirmación que debes aceptar

**Ejemplo de configuración Sandbox:**
```
Dominio Mailgun: sandboxe224a4604a554fda8cf51c1e2a935079.mailgun.org
Email remitente: postmaster@sandboxe224a4604a554fda8cf51c1e2a935079.mailgun.org
Email destinatario: TU_EMAIL@gmail.com (debe estar autorizado)
```

### 2. Configuración de Dominio Propio (Recomendado para Producción)

Para enviar emails a cualquier dirección sin restricciones:

1. **Agregar un dominio propio**:
   - Ir a Sending → Domains → Add New Domain
   - Ingresar tu dominio (ej: `mg.tudominio.com`)

2. **Configurar registros DNS** (en tu proveedor de DNS):
   ```
   TXT   mg.tudominio.com    v=spf1 include:mailgun.org ~all
   TXT   _domainkey.mg       [DKIM key proporcionado por Mailgun]
   CNAME email.mg            mailgun.org
   MX    mg.tudominio.com    mxa.mailgun.org (Priority: 10)
   MX    mg.tudominio.com    mxb.mailgun.org (Priority: 10)
   ```

3. **Verificar el dominio**: Mailgun verificará automáticamente los registros DNS

4. **Configurar en Ceiba**:
   ```
   Dominio Mailgun: mg.tudominio.com
   Email remitente: noreply@mg.tudominio.com
   Región: US o EU (según tu ubicación)
   ```

### 3. Verificación de API Key

Asegúrate de usar la API Key correcta:

1. Ir a **Settings → API Keys**
2. Copiar la **Private API Key** (empieza con `key-`)
3. **NO uses** la Public Validation Key

### 4. Formato del Email Remitente

Mailgun requiere el formato completo:
```
Nombre del Remitente <email@dominio.com>
```

Ceiba ya maneja esto automáticamente combinando:
- **Nombre del remitente**: Campo "FromName" en configuración
- **Email del remitente**: Campo "FromEmail" en configuración

## Configuración Recomendada en Ceiba

### Para Pruebas (Dominio Sandbox):

**Cómo encontrar tu dominio Sandbox:**
1. Ve a https://app.mailgun.com/
2. En el menú izquierdo, haz clic en "Sending" → "Domains"
3. Verás un dominio que empieza con "sandbox" (ejemplo: `sandboxe224a4604a554fda8cf51c1e2a935079.mailgun.org`)
4. Copia EXACTAMENTE ese dominio (incluye el prefijo "sandbox")

**Configuración en Ceiba:**
```
Proveedor: Mailgun
Habilitado: Sí
API Key: key-XXXXXXXXXXXXXXXXXXXXXXX (empieza con "key-")
Dominio: sandboxe224a4604a554fda8cf51c1e2a935079.mailgun.org
Región: US
Email remitente: postmaster@sandboxe224a4604a554fda8cf51c1e2a935079.mailgun.org
Nombre remitente: Ceiba - Reportes de Incidencias
```

**IMPORTANTE:**
- El dominio debe ser EXACTAMENTE como aparece en Mailgun Dashboard
- Autoriza el email de prueba en: Sending → Domain Settings → Authorized Recipients
```

### Para Producción (Dominio Propio):

```
Proveedor: Mailgun
Habilitado: Sí
API Key: [Tu API Key privada]
Dominio: mg.tudominio.com
Región: US (o EU según ubicación)
Email remitente: noreply@mg.tudominio.com
Nombre remitente: Ceiba - Reportes de Incidencias

IMPORTANTE: Verifica registros DNS antes de usar
```

## Prueba de Configuración

Después de configurar:

1. Ve a **Admin → Configuración de Email**
2. Habilita el servicio
3. Selecciona **Mailgun** como proveedor
4. Ingresa API Key, Dominio y Región
5. Guarda la configuración
6. Haz clic en **Probar Configuración**
7. Ingresa un email autorizado (si usas sandbox) o cualquier email (si usas dominio propio)

Si obtienes un error, el mensaje incluirá detalles específicos de Mailgun.

## Límites y Cuotas

### Plan Free (Sandbox):
- 5,000 emails/mes
- Solo a direcciones autorizadas (máximo 5)
- Bueno para desarrollo y testing

### Plan Flex (Producción):
- $35/mes por 50,000 emails
- Sin restricciones de destinatarios
- Dominio propio requerido

## Referencias

- Documentación oficial: https://documentation.mailgun.com/docs/mailgun/
- Dashboard: https://app.mailgun.com/
- API Reference: https://documentation.mailgun.com/docs/mailgun/api-reference/intro/
