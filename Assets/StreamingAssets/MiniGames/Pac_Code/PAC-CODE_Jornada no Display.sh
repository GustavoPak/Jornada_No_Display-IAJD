#!/bin/sh
printf '\033c\033]0;%s\a' PAC-CODE - Jornada no Display
base_path="$(dirname "$(realpath "$0")")"
"$base_path/PAC-CODE_Jornada no Display.x86_64" "$@"
