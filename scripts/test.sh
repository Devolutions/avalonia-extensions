#!/usr/bin/env bash
set -uo pipefail

summary_file="$(mktemp -t visual-regression-summary.XXXXXX)"
cleanup() {
  rm -f "$summary_file"
}
trap cleanup EXIT

dotnet test "$@" 2>&1 | while IFS= read -r line; do
  normalized="$line"

  if [[ "$normalized" =~ ^\[xUnit\.net[[:space:]][^]]+\][[:space:]]*(.*)$ ]]; then
    normalized="${BASH_REMATCH[1]}"
  fi

  normalized="${normalized#"${normalized%%[![:space:]]*}"}"

  if [[ "$normalized" =~ ^Visual\ regression\ detected\ for\ \[([^]]+)\]\ (.*)\ -\ ([^.]+)\.\ Diff\ saved\ to\ (.*)$ ]]; then
    row="$(printf '%s\t%s\t%s\t%s\t%s' "Visual regression" "${BASH_REMATCH[1]}" "${BASH_REMATCH[2]}" "${BASH_REMATCH[3]}" "${BASH_REMATCH[4]}")"
    grep -Fxq "$row" "$summary_file" || printf '%s\n' "$row" >> "$summary_file"
    continue
  fi

  if [[ "$normalized" =~ ^No\ baseline\ found\ for\ \[([^]]+)\]\ (.*)\ -\ ([^.]+)\.\ Saved\ screenshot\ to\ (.*)$ ]]; then
    row="$(printf '%s\t%s\t%s\t%s\t%s' "No baseline" "${BASH_REMATCH[1]}" "${BASH_REMATCH[2]}" "${BASH_REMATCH[3]}" "${BASH_REMATCH[4]}")"
    grep -Fxq "$row" "$summary_file" || printf '%s\n' "$row" >> "$summary_file"
    continue
  fi

  printf '%s\n' "$line"
done

dotnet_exit_code=${PIPESTATUS[0]}

if [[ -s "$summary_file" ]]; then
  printf '\n%s\n' "________________________________________________________________________________"
  printf '\033[33;1m%s\033[0m\n' "Visual regression summary"
  printf '\033[33;1m%-18s %-14s %-34s %-10s %s\033[0m\n' "Status" "Theme" "Page" "Variant" "Path"
  while IFS=$'\t' read -r status theme page variant path; do
    printf '\033[33;1m%-18s %-14s %-34s %-10s %s\033[0m\n' "$status" "[$theme]" "$page" "$variant" "$path"
  done < "$summary_file"
  printf '%s\n\n' "________________________________________________________________________________"
fi

exit "$dotnet_exit_code"
