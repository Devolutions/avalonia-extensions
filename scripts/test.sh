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

resolve_preset_filter() {
  local preset="$1"
  case "$preset" in
    visual)
      printf ''
      ;;
    nonvisual|non-visual|functional)
      printf ''
      ;;
    all)
      printf ''
      ;;
    *)
      return 1
      ;;
  esac
}

resolve_preset_project() {
  local preset="$1"
  case "$preset" in
    visual)
      printf 'tests/Devolutions.AvaloniaControls.VisualTests/Devolutions.AvaloniaControls.VisualTests.csproj'
      ;;
    nonvisual|non-visual|functional)
      printf 'tests/Devolutions.AvaloniaControls.Tests/Devolutions.AvaloniaControls.Tests.csproj'
      ;;
    *)
      return 1
      ;;
  esac
}

dotnet_args=()
has_logger_arg=0
has_filter_arg=0
preset_filter=""
preset_token=""
preset_project=""
update_baselines=0

if [[ $# -gt 0 && "$1" == "--update-baselines" ]]; then
  update_baselines=1
  shift
fi

if [[ $# -gt 0 ]]; then
  if resolved_preset_filter="$(resolve_preset_filter "$1")"; then
    preset_filter="$resolved_preset_filter"
    preset_token="$1"
    preset_project="$(resolve_preset_project "$1" || true)"
    shift
  fi
fi

while [[ $# -gt 0 ]]; do
  case "$1" in
    --update-baselines)
      update_baselines=1
      shift
      ;;
    --filter)
      has_filter_arg=1
      if [[ $# -lt 2 ]]; then
        dotnet_args+=("$1")
        shift
        continue
      fi
      dotnet_args+=("--filter" "$(normalize_filter_expression "$2")")
      shift 2
      ;;
    --filter=*)
      has_filter_arg=1
      dotnet_args+=("--filter=$(normalize_filter_expression "${1#--filter=}")")
      shift
      ;;
    --logger|-l)
      has_logger_arg=1
      dotnet_args+=("$1")
      if [[ $# -ge 2 ]]; then
        dotnet_args+=("$2")
        shift 2
      else
        shift
      fi
      ;;
    --logger=*|-l:*)
      has_logger_arg=1
      dotnet_args+=("$1")
      shift
      ;;
    *)
      dotnet_args+=("$1")
      shift
      ;;
  esac
done

if [[ "$has_filter_arg" -eq 1 && ( -n "$preset_project" || -n "$preset_filter" ) ]]; then
  printf '%s\n' "Cannot combine preset '$preset_token' with an explicit --filter. Use one or the other." >&2
  exit 1
fi

if [[ -n "$preset_filter" ]]; then
  dotnet_args+=("--filter" "$preset_filter")
fi

if [[ -n "$preset_project" ]]; then
  dotnet_args=("$preset_project" "${dotnet_args[@]}")
fi

if [[ "$has_logger_arg" -eq 0 ]]; then
  dotnet_args+=("--logger" "console;verbosity=normal")
fi

summary_file="$(mktemp -t visual-regression-summary.XXXXXX)"
result_line_file="$(mktemp -t visual-regression-result.XXXXXX)"
progress_seen_file="$(mktemp -t visual-regression-progress.XXXXXX)"
fallback_file="$(mktemp -t visual-regression-fallback.XXXXXX)"
totals_file="$(mktemp -t visual-regression-totals.XXXXXX)"
raw_output_file="$(mktemp -t visual-regression-output.XXXXXX)"
functional_failures_file="$(mktemp -t functional-test-failures.XXXXXX)"
last_progress_test=""
last_progress_status=""
cleanup() {
  rm -f "$summary_file"
  rm -f "$result_line_file"
  rm -f "$progress_seen_file"
  rm -f "$fallback_file"
  rm -f "$totals_file"
  rm -f "$raw_output_file"
  rm -f "$functional_failures_file"
}
trap cleanup EXIT

test_run_target=""
dotnet_env=()
if [[ "$update_baselines" -eq 1 ]]; then
  dotnet_env=(env UPDATE_BASELINES=true)
fi

"${dotnet_env[@]}" dotnet test ${dotnet_args[@]+"${dotnet_args[@]}"} 2>&1 | tee "$raw_output_file" | while IFS= read -r line; do
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

  if [[ "$normalized" =~ ^(Passed|Failed|Skipped)[[:space:]]+(.+)\[[^]]+\]$ ]]; then
    status_key="${BASH_REMATCH[1]}"
    test_name="${BASH_REMATCH[2]}"
    test_name="${test_name%[[:space:]]}"

    if [[ "$last_progress_test" == "$test_name" && "$last_progress_status" == "$status_key" ]]; then
      last_progress_test=""
      last_progress_status=""
      continue
    fi

    if [[ ! -s "$progress_seen_file" ]]; then
      printf 'Progress: '
      printf '1' > "$progress_seen_file"
    fi
    case "$status_key" in
      Passed) printf '✅' ;;
      Failed) printf '❌' ;;
      Skipped) printf 's' ;;
    esac
    last_progress_test="$test_name"
    last_progress_status="$status_key"

    if [[ "$status_key" == "Failed" ]]; then
      if [[ "$test_name" == *"VisualRegressionTests"* ]]; then
        current_failed_test=""
      else
        current_failed_test="$test_name"
      fi
      current_failed_message=""
      in_error_message_block=0
    fi
    continue
  fi

  if [[ "$normalized" =~ ^(Failed!|Passed!)[[:space:]]+-[[:space:]]+Failed:[[:space:]]+[0-9]+,[[:space:]]+Passed:[[:space:]]+[0-9]+,[[:space:]]+Skipped:[[:space:]]+[0-9]+,[[:space:]]+Total:[[:space:]]+[0-9]+, ]]; then
    printf '%s\n' "$normalized" > "$result_line_file"
    continue
  fi

  if [[ "$normalized" =~ ^Failed[[:space:]]+(.+)\[[^]]+\]$ ]]; then
    if [[ "${BASH_REMATCH[1]}" == *"VisualRegressionTests"* ]]; then
      continue
    fi
    current_failed_test="${BASH_REMATCH[1]}"
    current_failed_test="${current_failed_test%[[:space:]]}"
    current_failed_message=""
    in_error_message_block=0
    continue
  fi

  if [[ "$normalized" =~ ^Test\ run\ for\ (.+)\ \((.+)\)$ ]]; then
    test_run_target="${BASH_REMATCH[1]} ${BASH_REMATCH[2]}"
    printf 'target=%s\n' "$test_run_target" > "$totals_file"
    continue
  fi

  if [[ "$normalized" =~ ^Test\ Run\ (Successful|Failed)\.$ ]]; then
    printf 'status=%s\n' "${BASH_REMATCH[1]}" >> "$totals_file"
    continue
  fi

  if [[ "$normalized" =~ ^Total\ tests:[[:space:]]+([0-9]+)$ ]]; then
    printf 'total=%s\n' "${BASH_REMATCH[1]}" >> "$totals_file"
    continue
  fi

  if [[ "$normalized" =~ ^Passed:[[:space:]]+([0-9]+)$ ]]; then
    printf 'passed=%s\n' "${BASH_REMATCH[1]}" >> "$totals_file"
    continue
  fi

  if [[ "$normalized" =~ ^Failed:[[:space:]]+([0-9]+)$ ]]; then
    printf 'failed=%s\n' "${BASH_REMATCH[1]}" >> "$totals_file"
    continue
  fi

  if [[ "$normalized" =~ ^Skipped:[[:space:]]+([0-9]+)$ ]]; then
    printf 'skipped=%s\n' "${BASH_REMATCH[1]}" >> "$totals_file"
    continue
  fi

  if [[ "$normalized" =~ ^Total\ time:[[:space:]]+(.+)$ ]]; then
    printf 'duration=%s\n' "${BASH_REMATCH[1]}" >> "$totals_file"
    continue
  fi

  if [[ -z "$normalized" ]]; then
    continue
  fi

  if [[ "$normalized" =~ ^(Determining\ projects\ to\ restore|All\ projects\ are\ up-to-date\ for\ restore|Test\ run\ for\ |VSTest\ version|Starting\ test\ execution,\ please\ wait|A\ total\ of\ [0-9]+\ test\ files\ matched\ the\ specified\ pattern\.) ]]; then
    continue
  fi

  if [[ "$normalized" =~ ^\[xUnit\.net[[:space:]] ]]; then
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

  if [[ "$normalized" =~ warning[[:space:]]+[A-Z]{2,}[0-9]+: ]]; then
    continue
  fi

  printf '%s\n' "$line" >> "$fallback_file"
done

dotnet_exit_code=${PIPESTATUS[0]}

# Parse the raw output after the run so we can print a readable functional failure list regardless of xUnit formatting details.
if [[ -s "$raw_output_file" ]]; then
  awk '
    BEGIN { current=""; message=""; in_error=0 }
    /^[[:space:]]*Failed[[:space:]]+.*\[[^][]+\][[:space:]]*$/ {
      current = $0
      sub(/^[[:space:]]*Failed[[:space:]]+/, "", current)
      sub(/[[:space:]]+\[[^][]+\][[:space:]]*$/, "", current)
      if (current ~ /VisualRegressionTests/) {
        current = ""
        message = ""
        in_error = 0
        next
      }
      if (message != "") {
        print current "\t" message
      }
      message = ""
      in_error = 0
      next
    }
    /^[[:space:]]*No baseline found for / || /^[[:space:]]*Visual regression detected for / {
      current = ""
      message = ""
      in_error = 0
      next
    }
    /^[[:space:]]*Error Message:[[:space:]]*$/ {
      if (current == "") {
        next
      }
      in_error = 1
      next
    }
    in_error {
      if ($0 ~ /^[[:space:]]*Stack Trace:[[:space:]]*$/) {
        if (current != "" && message != "") {
          print current "\t" message
        }
        current = ""
        message = ""
        in_error = 0
        next
      }
      line = $0
      sub(/^[[:space:]]+/, "", line)
      if (line == "" || line ~ /^at /) {
        next
      }
      if (message != "") {
        message = message " | "
      }
      message = message line
      next
    }
    END {
      if (current != "" && message != "") {
        print current "\t" message
      }
    }
  ' "$raw_output_file" > "$functional_failures_file"
fi

if [[ -s "$progress_seen_file" ]]; then
  printf '\n'
fi

if [[ -s "$result_line_file" ]]; then
  cat "$result_line_file"
elif [[ -s "$totals_file" ]]; then
  status_value="$(awk -F= '/^status=/{v=$2} END{print v}' "$totals_file")"
  total_value="$(awk -F= '/^total=/{v=$2} END{print v}' "$totals_file")"
  passed_value="$(awk -F= '/^passed=/{v=$2} END{print v}' "$totals_file")"
  failed_value="$(awk -F= '/^failed=/{v=$2} END{print v}' "$totals_file")"
  skipped_value="$(awk -F= '/^skipped=/{v=$2} END{print v}' "$totals_file")"
  duration_value="$(awk -F= '/^duration=/{v=$2} END{print v}' "$totals_file")"
  target_value="$(awk -F= '/^target=/{v=substr($0,8)} END{print v}' "$totals_file")"

  [[ -z "$failed_value" ]] && failed_value=0
  [[ -z "$passed_value" ]] && passed_value=0
  [[ -z "$skipped_value" ]] && skipped_value=0
  [[ -z "$total_value" ]] && total_value=0

  if [[ "$status_value" == "Successful" ]]; then
    status_label="Passed!"
  else
    status_label="Failed!"
  fi

  printf "%s  - Failed: %5s, Passed: %5s, Skipped: %5s, Total: %5s, Duration: %s - %s\n" \
    "$status_label" "$failed_value" "$passed_value" "$skipped_value" "$total_value" "${duration_value:-unknown}" "${target_value:-dotnet test}"
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

if [[ -s "$functional_failures_file" ]]; then
  printf '\n%s\n' "________________________________________________________________________________"
  printf '\033[1m%s\033[0m\n' "Functional test failures"
  while IFS=$'\t' read -r test_name failure_message; do
    printf '\033[1m• %s\033[0m\n' "$test_name"
    printf '  %s\n' "$failure_message"
  done < "$functional_failures_file"
  printf '%s\n\n' "________________________________________________________________________________"
fi

exit "$dotnet_exit_code"
