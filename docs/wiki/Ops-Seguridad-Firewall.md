# Configuración de Firewall

Esta guía describe cómo configurar el firewall para proteger el servidor de Ceiba.

## Puertos Requeridos

| Puerto | Servicio | Acceso |
|--------|----------|--------|
| 22 | SSH | Restringido (VPN/IPs específicas) |
| 80 | HTTP | Público (redirige a HTTPS) |
| 443 | HTTPS | Público |
| 5432 | PostgreSQL | Solo interno |

## Firewalld (Fedora/RHEL)

### Configuración Básica

```bash
# Verificar estado
sudo systemctl status firewalld

# Habilitar si no está activo
sudo systemctl enable --now firewalld

# Ver zona activa
sudo firewall-cmd --get-active-zones

# Ver reglas actuales
sudo firewall-cmd --list-all
```

### Configurar Puertos

```bash
# Permitir HTTP y HTTPS
sudo firewall-cmd --permanent --add-service=http
sudo firewall-cmd --permanent --add-service=https

# Permitir SSH (ya debería estar)
sudo firewall-cmd --permanent --add-service=ssh

# NO exponer PostgreSQL externamente
# sudo firewall-cmd --permanent --add-port=5432/tcp  # NO HACER

# Aplicar cambios
sudo firewall-cmd --reload
```

### Restringir SSH por IP

```bash
# Crear zona rica para SSH restringido
sudo firewall-cmd --permanent --new-zone=ssh-restricted
sudo firewall-cmd --permanent --zone=ssh-restricted --add-source=10.0.0.0/8
sudo firewall-cmd --permanent --zone=ssh-restricted --add-service=ssh

# Remover SSH de zona pública
sudo firewall-cmd --permanent --zone=public --remove-service=ssh

sudo firewall-cmd --reload
```

### Reglas Avanzadas

```bash
# Limitar conexiones por IP (anti DDoS básico)
sudo firewall-cmd --permanent --add-rich-rule='
  rule family="ipv4"
  service name="https"
  limit value="25/m"
  accept'

# Bloquear IP específica
sudo firewall-cmd --permanent --add-rich-rule='
  rule family="ipv4"
  source address="1.2.3.4"
  reject'

# Permitir solo rango específico para admin
sudo firewall-cmd --permanent --add-rich-rule='
  rule family="ipv4"
  source address="192.168.1.0/24"
  port protocol="tcp" port="22"
  accept'

sudo firewall-cmd --reload
```

## UFW (Ubuntu)

### Configuración Básica

```bash
# Instalar si no está
sudo apt install ufw

# Configurar políticas por defecto
sudo ufw default deny incoming
sudo ufw default allow outgoing

# Permitir SSH antes de habilitar
sudo ufw allow ssh

# Habilitar firewall
sudo ufw enable

# Ver estado
sudo ufw status verbose
```

### Configurar Puertos

```bash
# Permitir HTTP y HTTPS
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp

# O por nombre de servicio
sudo ufw allow http
sudo ufw allow https

# Ver reglas
sudo ufw status numbered
```

### Restringir SSH

```bash
# Permitir SSH solo desde red interna
sudo ufw allow from 10.0.0.0/8 to any port 22
sudo ufw delete allow ssh  # Eliminar regla general

# Limitar intentos de conexión SSH
sudo ufw limit ssh
```

### Reglas Específicas

```bash
# Permitir IP específica
sudo ufw allow from 203.0.113.50

# Bloquear IP
sudo ufw deny from 1.2.3.4

# Permitir rango de IPs a puerto específico
sudo ufw allow from 192.168.1.0/24 to any port 443

# Eliminar regla por número
sudo ufw status numbered
sudo ufw delete 5
```

## iptables (Manual)

### Script de Configuración

```bash
#!/bin/bash
# /etc/iptables/rules.sh

# Limpiar reglas existentes
iptables -F
iptables -X

# Políticas por defecto
iptables -P INPUT DROP
iptables -P FORWARD DROP
iptables -P OUTPUT ACCEPT

# Permitir loopback
iptables -A INPUT -i lo -j ACCEPT
iptables -A OUTPUT -o lo -j ACCEPT

# Permitir conexiones establecidas
iptables -A INPUT -m state --state ESTABLISHED,RELATED -j ACCEPT

# Permitir SSH (con rate limiting)
iptables -A INPUT -p tcp --dport 22 -m state --state NEW -m recent --set
iptables -A INPUT -p tcp --dport 22 -m state --state NEW -m recent --update --seconds 60 --hitcount 4 -j DROP
iptables -A INPUT -p tcp --dport 22 -j ACCEPT

# Permitir HTTP/HTTPS
iptables -A INPUT -p tcp --dport 80 -j ACCEPT
iptables -A INPUT -p tcp --dport 443 -j ACCEPT

# Permitir ICMP (ping)
iptables -A INPUT -p icmp --icmp-type echo-request -j ACCEPT

# Log de paquetes rechazados
iptables -A INPUT -j LOG --log-prefix "IPTABLES-DROP: "

# Guardar reglas
iptables-save > /etc/iptables/rules.v4
```

## Fail2ban

### Instalación

```bash
# Fedora/RHEL
sudo dnf install fail2ban

# Ubuntu/Debian
sudo apt install fail2ban

# Habilitar
sudo systemctl enable --now fail2ban
```

### Configuración para SSH

```ini
# /etc/fail2ban/jail.local
[DEFAULT]
bantime = 1h
findtime = 10m
maxretry = 5
ignoreip = 127.0.0.1/8 10.0.0.0/8

[sshd]
enabled = true
port = ssh
filter = sshd
logpath = /var/log/auth.log  # Ubuntu
# logpath = /var/log/secure  # Fedora
maxretry = 3
bantime = 24h
```

### Configuración para Nginx

```ini
# /etc/fail2ban/jail.local
[nginx-http-auth]
enabled = true
port = http,https
filter = nginx-http-auth
logpath = /var/log/nginx/error.log

[nginx-limit-req]
enabled = true
port = http,https
filter = nginx-limit-req
logpath = /var/log/nginx/error.log
maxretry = 10

[nginx-botsearch]
enabled = true
port = http,https
filter = nginx-botsearch
logpath = /var/log/nginx/access.log
maxretry = 2
```

### Comandos Útiles

```bash
# Ver estado
sudo fail2ban-client status

# Ver jail específico
sudo fail2ban-client status sshd

# Desbanear IP
sudo fail2ban-client set sshd unbanip 1.2.3.4

# Ver IPs baneadas
sudo fail2ban-client get sshd banned
```

## Docker y Firewall

### Problema con Docker y iptables

Docker modifica iptables directamente, lo que puede bypasear el firewall.

### Solución

```json
// /etc/docker/daemon.json
{
  "iptables": false
}
```

```bash
# Reiniciar Docker
sudo systemctl restart docker
```

Luego configurar manualmente las reglas para Docker.

### Alternativa: Usar Red Host

```yaml
# docker-compose.yml
services:
  ceiba-web:
    network_mode: host  # Usa firewall del host
```

## Verificación

### Escanear Puertos

```bash
# Desde otra máquina
nmap -sT servidor.com

# Verificar puertos abiertos localmente
ss -tlnp

# Verificar reglas de firewall
sudo iptables -L -n -v  # iptables
sudo firewall-cmd --list-all  # firewalld
sudo ufw status verbose  # ufw
```

### Probar Conexiones

```bash
# Verificar HTTP/HTTPS accesible
curl -I https://ceiba.tudominio.com

# Verificar SSH funciona
ssh usuario@servidor -v

# Verificar PostgreSQL NO accesible externamente
nc -zv servidor 5432  # Debería fallar desde afuera
```

## Próximos Pasos

- [Hardening del sistema](Ops-Seguridad-Hardening)
- [Configurar HTTPS](Ops-Config-SSL-HTTPS)
- [Plan de respuesta a incidentes](Ops-Seguridad-Incidentes)
