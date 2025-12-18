#!/bin/bash
# sync-wiki.sh
# Script para sincronizar docs/wiki/ con el GitHub Wiki
#
# Uso:
#   ./scripts/sync-wiki.sh [push|pull]
#
# Prerrequisitos:
#   - Git instalado
#   - Acceso al repositorio wiki de GitHub
#   - SSH key configurada o token de acceso
#
# El wiki de GitHub es un repositorio separado con URL:
#   git@github.com:montfort/ri-ceiba.wiki.git
#
# Nota: El wiki debe estar habilitado en GitHub para que el repositorio exista.
# Ve a Settings > Features > Wikis en tu repositorio de GitHub.

set -e

# Configuración
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
WIKI_SOURCE="$PROJECT_ROOT/docs/wiki"
WIKI_CLONE_DIR="$PROJECT_ROOT/.wiki-temp"

# URL del wiki (repo principal + .wiki.git)
# Usar HTTPS para compatibilidad con autenticación por token/credential helper
WIKI_REPO_URL="${WIKI_REPO_URL:-https://github.com/montfort/ri-ceiba.wiki.git}"

# Colores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Verificar que existe el directorio fuente
check_source() {
    if [ ! -d "$WIKI_SOURCE" ]; then
        log_error "No se encontró el directorio $WIKI_SOURCE"
        exit 1
    fi
}

# Clonar o actualizar el wiki
clone_wiki() {
    if [ -d "$WIKI_CLONE_DIR" ]; then
        log_info "Actualizando copia local del wiki..."
        cd "$WIKI_CLONE_DIR"
        git fetch origin
        git reset --hard origin/master 2>/dev/null || git reset --hard origin/main
    else
        log_info "Clonando wiki desde $WIKI_REPO_URL..."
        git clone "$WIKI_REPO_URL" "$WIKI_CLONE_DIR"
    fi
}

# Sincronizar docs/wiki/ -> GitHub Wiki (push)
push_to_wiki() {
    check_source
    clone_wiki

    log_info "Copiando archivos de $WIKI_SOURCE a $WIKI_CLONE_DIR..."

    # Limpiar contenido anterior (excepto .git)
    find "$WIKI_CLONE_DIR" -mindepth 1 -maxdepth 1 ! -name '.git' -exec rm -rf {} +

    # Copiar archivos nuevos
    cp -r "$WIKI_SOURCE"/* "$WIKI_CLONE_DIR/"

    cd "$WIKI_CLONE_DIR"

    # Verificar si hay cambios
    if git diff --quiet && git diff --staged --quiet; then
        # Verificar archivos no trackeados
        if [ -z "$(git status --porcelain)" ]; then
            log_info "No hay cambios para sincronizar"
            return 0
        fi
    fi

    # Agregar y commitear
    git add -A

    # Mostrar resumen de cambios
    log_info "Cambios a sincronizar:"
    git status --short

    # Crear commit
    COMMIT_MSG="Sync wiki from docs/wiki/ - $(date '+%Y-%m-%d %H:%M:%S')"
    git commit -m "$COMMIT_MSG"

    # Push
    log_info "Subiendo cambios al wiki de GitHub..."
    git push origin master 2>/dev/null || git push origin main

    log_info "✓ Wiki sincronizado exitosamente"
}

# Sincronizar GitHub Wiki -> docs/wiki/ (pull)
pull_from_wiki() {
    clone_wiki

    log_info "Copiando archivos de $WIKI_CLONE_DIR a $WIKI_SOURCE..."

    # Crear directorio destino si no existe
    mkdir -p "$WIKI_SOURCE"

    # Limpiar contenido anterior
    find "$WIKI_SOURCE" -mindepth 1 -maxdepth 1 -exec rm -rf {} +

    # Copiar archivos (excluyendo .git)
    find "$WIKI_CLONE_DIR" -mindepth 1 -maxdepth 1 ! -name '.git' -exec cp -r {} "$WIKI_SOURCE/" \;

    log_info "✓ Contenido del wiki descargado a $WIKI_SOURCE"
    log_info "Recuerda hacer commit de los cambios en el repositorio principal"
}

# Limpiar directorio temporal
cleanup() {
    if [ -d "$WIKI_CLONE_DIR" ]; then
        log_info "Limpiando directorio temporal..."
        rm -rf "$WIKI_CLONE_DIR"
    fi
}

# Mostrar ayuda
show_help() {
    echo "Uso: $0 [comando]"
    echo ""
    echo "Comandos:"
    echo "  push    Sincronizar docs/wiki/ -> GitHub Wiki"
    echo "  pull    Sincronizar GitHub Wiki -> docs/wiki/"
    echo "  clean   Limpiar directorio temporal"
    echo "  help    Mostrar esta ayuda"
    echo ""
    echo "Variables de entorno:"
    echo "  WIKI_REPO_URL   URL del repositorio wiki (default: https://github.com/montfort/ri-ceiba.wiki.git)"
    echo ""
    echo "Ejemplos:"
    echo "  $0 push                                    # Subir cambios al wiki"
    echo "  WIKI_REPO_URL=git@github.com:otro/repo.wiki.git $0 push"
}

# Main
case "${1:-help}" in
    push)
        push_to_wiki
        ;;
    pull)
        pull_from_wiki
        ;;
    clean)
        cleanup
        ;;
    help|--help|-h)
        show_help
        ;;
    *)
        log_error "Comando desconocido: $1"
        show_help
        exit 1
        ;;
esac
