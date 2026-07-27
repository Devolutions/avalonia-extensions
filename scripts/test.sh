#!/usr/bin/env bash
set -uo pipefail

normalize_filter_expression() {
  local expression="$1"

  if [[ "$expression" == *"~"* ||
        "$expression" == *"="* ||
        "$expression" == *"!"* ||
        "$expression" == *"<"* ||
        "$expression" == *">"* ||
        "$expression" == *"|"* ||
        "$expression" == *"&"* ||
        "$expression" == *"("* ||
        "$expression" == *")"* ]]; then
    printf '%s' "$expression"
    return
  fi

  printf 'DisplayName~%s' "$expression"
}

dotnet_args=()
while [[ $# -gt 0 ]]; do
  case "$1" in
    --filter)
      if [[ $# -lt 2 ]]; then
        dotnet_args+=("$1")
        shift
        continue
      fi
      dotnet_args+=("--filter" "$(normalize_filter_expression "$2")")
      shift 2
      ;;
    --filter=*)
      dotnet_args+=("--filter=$(normalize_filter_expression "${1#--filter=}")")
      shift
      ;;
    *)
      dotnet_args+=("$1")
      shift
      ;;
  esac
done

summary_file="$(mktemp -t visual-regression-summary.XXXXXX)"
result_line_file="$(mktemp -t visual-regression-result.XXXXXX)"
progress_seen_file="$(mktemp -t visual-regression-progress.XXXXXX)"
fallback_file="$(mktemp -t visual-regression-fallback.XXXXXX)"
cleanup() {
  rm -f "$summary_file"
  rm -f "$result_line_file"
  rm -f "$progress_seen_file"
  rm -f "$fallback_file"
}
trap cleanup EXIT

dotnet test ${dotnet_args[@]+"${dotnet_args[@]}"} 2>&1 | while IFS= read -r line; do
  normalized="$line"

  if [[ "$normalized" =~ ^\[xUnit\.net[[:space:]][^]]+\][[:space:]]*(.*)$ ]]; then
    normalized="${BASH_REMATCH[1]}"
  fi

  normalized="${normalized#"${normalized%%[![:space:]]*}"}"

  if [[ "$normalized" =~ ^Visual\ regression\ detected\ for\ \[([^]]+)\]\ (.*)\ -\ ([^.]+)\.(\ DesiredH=([0-9]+(\.[0-9]+)?)\.)?\ Diff\ saved\ to\ (.*)$ ]]; then
    capped_desired_height="${BASH_REMATCH[5]}"
    row="$(printf '%s\t%s\t%s\t%s\t%s\t%s' "Visual regression" "${BASH_REMATCH[1]}" "${BASH_REMATCH[2]}" "${BASH_REMATCH[3]}" "${BASH_REMATCH[7]}" "$capped_desired_height")"
    grep -Fxq "$row" "$summary_file" || printf '%s\n' "$row" >> "$summary_file"
    continue
  fi

  if [[ "$normalized" =~ ^No\ baseline\ found\ for\ \[([^]]+)\]\ (.*)\ -\ ([^.]+)\.(\ DesiredH=([0-9]+(\.[0-9]+)?)\.)?\ Saved\ screenshot\ to\ (.*)$ ]]; then
    capped_desired_height="${BASH_REMATCH[5]}"
    row="$(printf '%s\t%s\t%s\t%s\t%s\t%s' "No baseline" "${BASH_REMATCH[1]}" "${BASH_REMATCH[2]}" "${BASH_REMATCH[3]}" "${BASH_REMATCH[7]}" "$capped_desired_height")"
    grep -Fxq "$row" "$summary_file" || printf '%s\n' "$row" >> "$summary_file"
    continue
  fi

  if [[ "$normalized" =~ \[(FAIL|PASS|SKIP)\]$ ]]; then
    if [[ ! -s "$progress_seen_file" ]]; then
      printf 'Progress: '
      printf '1' > "$progress_seen_file"
    fi
    printf '>'
    continue
  fi

  if [[ "$normalized" =~ ^(Failed!|Passed!)[[:space:]]+-[[:space:]]+Failed:[[:space:]]+[0-9]+,[[:space:]]+Passed:[[:space:]]+[0-9]+,[[:space:]]+Skipped:[[:space:]]+[0-9]+,[[:space:]]+Total:[[:space:]]+[0-9]+, ]]; then
    printf '%s\n' "$normalized" > "$result_line_file"
    continue
  fi

  if [[ -z "$normalized" ]]; then
    continue
  fi

  if [[ "$normalized" =~ ^(Determining\ projects\ to\ restore|All\ projects\ are\ up-to-date\ for\ restore|Test\ run\ for\ |VSTest\ version|Starting\ test\ execution,\ please\ wait|A\ total\ of\ [0-9]+\ test\ files\ matched\ the\ specified\ pattern\.) ]]; then
    continue
  fi

  if [[ "$normalized" =~ ^at[[:space:]] ]]; then
    continue
  fi

  if [[ "$normalized" =~ ^(Stack\ Trace:|Error\ Message:)$ ]]; then
    continue
  fi

  if [[ "$normalized" =~ ^[A-Za-z0-9._-]+[[:space:]]+\-\>[[:space:]] ]]; then
    continue
  fi

  if [[ "$normalized" =~ ^/.*:[[:digit:]]+:[[:digit:]]+:[[:space:]]warning[[:space:]] ]]; then
    continue
  fi

  printf '%s\n' "$line" >> "$fallback_file"
done

dotnet_exit_code=${PIPESTATUS[0]}

if [[ -s "$progress_seen_file" ]]; then
  printf '\n'
fi

if [[ -s "$result_line_file" ]]; then
  cat "$result_line_file"
elif [[ -s "$fallback_file" ]]; then
  cat "$fallback_file"
fi

if [[ -s "$summary_file" ]]; then
  printf '\n%s\n' "________________________________________________________________________________"
  printf '\033[33;1m%s\033[0m\n' "Visual regression summary"
  printf '\033[33;1m%-18s %-14s %-34s %-10s %-8s %s\033[0m\n' "Status" "Theme" "Page" "Variant" "DesiredH" "Path"
  while IFS=$'\t' read -r status theme page variant path capped_desired_height; do
    printf '\033[33;1m%-18s %-14s %-34s %-10s %-8s %s\033[0m\n' "$status" "[$theme]" "$page" "$variant" "${capped_desired_height:-}" "$path"
  done < "$summary_file"
  printf '%s\n\n' "________________________________________________________________________________"
fi

exit "$dotnet_exit_code"
