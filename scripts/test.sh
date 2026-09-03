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
  dotnet_args=("$preset_project" "${dotnet_args[@]+"${dotnet_args[@]}"}")
fi

if [[ "$has_logger_arg" -eq 0 ]]; then
  dotnet_args+=("--logger" "console;verbosity=normal")
fi

summary_file="$(mktemp -t visual-regression-summary.XXXXXX)"
result_line_file="$(mktemp -t visual-regression-result.XXXXXX)"
fallback_file="$(mktemp -t visual-regression-fallback.XXXXXX)"
totals_file="$(mktemp -t visual-regression-totals.XXXXXX)"
raw_output_file="$(mktemp -t visual-regression-output.XXXXXX)"
functional_failures_file="$(mktemp -t functional-test-failures.XXXXXX)"
test_list_file="$(mktemp -t test-list.XXXXXX)"
test_exit_file="$(mktemp -t test-exit-code.XXXXXX)"
last_progress_test=""
last_progress_status=""
progress_count=0
passed_count=0
failed_count=0
skipped_count=0
total_tests=0
progress_bar_width=30
progress_number_width=6
initialization_done_text='Initializing test run... done!'
flower_frames=('✻' '✽' '✶' '✳' '✢')
flower_index=0
flower_column=$(( ${#initialization_done_text} + 2 ))
flower_tick=$'\036devtest-flower-tick'

print_progress() {
  local completed=0
  local remaining=$progress_bar_width

  if [[ "$total_tests" -gt 0 ]]; then
    completed=$((progress_count * progress_bar_width / total_tests))
    remaining=$((progress_bar_width - completed))
  fi

  printf '\033[1GProgress: %*d/%-*s [' "$progress_number_width" "$progress_count" "$progress_number_width" "$progress_total"
  printf '%*s' "$completed" '' | tr ' ' '#'
  printf '%*s' "$remaining" '' | tr ' ' '-'
  printf '] ok:%4d - fail:%3d - skip:%3d' "$passed_count" "$failed_count" "$skipped_count"
}

animate_flower() {
  flower_index=$(( (flower_index + 1) % ${#flower_frames[@]} ))
  printf '\033[1A\033[%dG%s\033[1B' "$flower_column" "${flower_frames[$flower_index]}"
}

cleanup() {
  if [[ "${cursor_hidden:-0}" -eq 1 ]]; then
    printf '\033[?25h'
  fi
  rm -f "$summary_file"
  rm -f "$result_line_file"
  rm -f "$fallback_file"
  rm -f "$totals_file"
  rm -f "$raw_output_file"
  rm -f "$functional_failures_file"
  rm -f "$test_list_file"
  rm -f "$test_exit_file"
}
trap cleanup EXIT

test_run_target=""
dotnet_env=()
if [[ "$update_baselines" -eq 1 ]]; then
  dotnet_env=(env UPDATE_BASELINES=true)
fi

spinner_frames=('⠋' '⠙' '⠸' '⠴' '⠦' '⠇')
spinner_index=0
cursor_hidden=1
printf '\033[?25lInitializing test run... %s\n' "${spinner_frames[$spinner_index]}"
"${dotnet_env[@]+"${dotnet_env[@]}"}" dotnet test ${dotnet_args[@]+"${dotnet_args[@]}"} --list-tests > "$test_list_file" 2>&1 &
discovery_pid=$!
while kill -0 "$discovery_pid" 2>/dev/null; do
  spinner_index=$(( (spinner_index + 1) % ${#spinner_frames[@]} ))
  printf '\033[1A\rInitializing test run... %s\033[K\033[1B\r' "${spinner_frames[$spinner_index]}"
  sleep 0.1
done

if wait "$discovery_pid"; then
  total_tests="$(awk '/^    [^[:space:]]/ { count++ } END { print count + 0 }' "$test_list_file")"
fi
expected_project_count="$(awk '/^[[:space:]]*Test run for / { count++ } END { print count + 0 }' "$test_list_file")"
printf '\033[1A\033[1G\033[2K%s %s\033[1B\033[1G' "$initialization_done_text" "${flower_frames[$flower_index]}"

if [[ "$total_tests" -gt 0 ]]; then
  progress_total="$total_tests"
  progress_number_width="${#total_tests}"
else
  progress_total='?'
fi
progress_count=0
print_progress

# Timer emits parser events instead of writing to the terminal, keeping cursor updates single-threaded.
{
  (
    "${dotnet_env[@]+"${dotnet_env[@]}"}" dotnet test ${dotnet_args[@]+"${dotnet_args[@]}"} 2>&1 | tee "$raw_output_file"
    printf '%s\n' "${PIPESTATUS[0]}" > "$test_exit_file"
  ) &
  test_pid=$!

  (
    while kill -0 "$test_pid" 2>/dev/null; do
      sleep 0.35
      if kill -0 "$test_pid" 2>/dev/null; then
        printf '%s\n' "$flower_tick"
      fi
    done
  ) &
  timer_pid=$!

  wait "$test_pid"
  wait "$timer_pid"
} | while IFS= read -r line; do
  if [[ "$line" == "$flower_tick" ]]; then
    animate_flower
    continue
  fi

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

    progress_count=$((progress_count + 1))
    case "$status_key" in
      Passed) passed_count=$((passed_count + 1)) ;;
      Failed) failed_count=$((failed_count + 1)) ;;
      Skipped) skipped_count=$((skipped_count + 1)) ;;
    esac
    print_progress
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
    printf '%s\n' "$normalized" >> "$result_line_file"
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
    printf 'target=%s\n' "$test_run_target" >> "$totals_file"
    continue
  fi

  if [[ "$normalized" =~ ^Test\ Run\ (Successful|Failed)\.$ ]]; then
    printf 'record\n' >> "$totals_file"
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

dotnet_exit_code="$(< "$test_exit_file")"
if [[ -z "$dotnet_exit_code" ]]; then
  dotnet_exit_code=1
fi

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

printf '\033[1G\n'

partial_run=0
result_project_count="$(wc -l < "$result_line_file" | tr -d ' ')"
legacy_project_count="$(grep -c '^record$' "$totals_file" || true)"
reported_project_count="$result_project_count"
if [[ "$legacy_project_count" -gt "$reported_project_count" ]]; then
  reported_project_count="$legacy_project_count"
fi
if [[ "$dotnet_exit_code" -ne 0 ]] &&
   { [[ "$expected_project_count" -eq 0 ]] || [[ "$reported_project_count" -lt "$expected_project_count" ]]; }; then
  partial_run=1
fi

if [[ "$partial_run" -eq 1 ]]; then
  if [[ -s "$fallback_file" ]]; then
    cat "$fallback_file"
  fi
  printf 'Failed! - dotnet test exited with code %s before all project summaries were produced.\n' "$dotnet_exit_code"
elif [[ -s "$result_line_file" ]]; then
  result_line_count="$(wc -l < "$result_line_file" | tr -d ' ')"
  if [[ "$result_line_count" -eq 1 ]]; then
    cat "$result_line_file"
  else
    aggregate_failed=0
    aggregate_passed=0
    aggregate_skipped=0
    aggregate_total=0
    aggregate_duration_seconds=0
    aggregate_parse_succeeded=1

    while IFS= read -r result_line; do
      if [[ "$result_line" =~ Failed:[[:space:]]+([0-9]+),[[:space:]]+Passed:[[:space:]]+([0-9]+),[[:space:]]+Skipped:[[:space:]]+([0-9]+),[[:space:]]+Total:[[:space:]]+([0-9]+),[[:space:]]+Duration:[[:space:]]+([0-9.]+)[[:space:]]+(Seconds|Minutes|Hours) ]]; then
        aggregate_failed=$((aggregate_failed + BASH_REMATCH[1]))
        aggregate_passed=$((aggregate_passed + BASH_REMATCH[2]))
        aggregate_skipped=$((aggregate_skipped + BASH_REMATCH[3]))
        aggregate_total=$((aggregate_total + BASH_REMATCH[4]))
        duration_value="${BASH_REMATCH[5]}"
        case "${BASH_REMATCH[6]}" in
          Hours) aggregate_duration_seconds="$(awk -v value="$aggregate_duration_seconds" -v duration="$duration_value" 'BEGIN { printf "%.4f", value + duration * 3600 }')" ;;
          Minutes) aggregate_duration_seconds="$(awk -v value="$aggregate_duration_seconds" -v duration="$duration_value" 'BEGIN { printf "%.4f", value + duration * 60 }')" ;;
          Seconds) aggregate_duration_seconds="$(awk -v value="$aggregate_duration_seconds" -v duration="$duration_value" 'BEGIN { printf "%.4f", value + duration }')" ;;
        esac
      else
        aggregate_parse_succeeded=0
        break
      fi
    done < "$result_line_file"

    if [[ "$aggregate_parse_succeeded" -eq 1 ]]; then
      if [[ "$aggregate_failed" -eq 0 ]]; then
        aggregate_status_label="Passed!"
      else
        aggregate_status_label="Failed!"
      fi
      printf "%s  - Failed: %5s, Passed: %5s, Skipped: %5s, Total: %5s, Duration: %.4f Seconds (%s projects) - dotnet test\n" \
        "$aggregate_status_label" "$aggregate_failed" "$aggregate_passed" "$aggregate_skipped" "$aggregate_total" "$aggregate_duration_seconds" "$result_line_count"
    else
      cat "$result_line_file"
    fi
  fi
elif [[ -s "$totals_file" && "$(grep -c '^record$' "$totals_file")" -gt 1 ]]; then
  aggregate_values="$(awk -F= '
    $0 == "record" { records++; next }
    $1 == "total" { total += $2; next }
    $1 == "passed" { passed += $2; next }
    $1 == "failed" { failed += $2; next }
    $1 == "skipped" { skipped += $2; next }
    END { printf "%d %d %d %d %d", failed, passed, skipped, total, records }
  ' "$totals_file")"
  read -r aggregate_failed aggregate_passed aggregate_skipped aggregate_total aggregate_projects <<< "$aggregate_values"
  if [[ "$aggregate_failed" -eq 0 ]]; then
    aggregate_status_label="Passed!"
  else
    aggregate_status_label="Failed!"
  fi
  printf "%s  - Failed: %5s, Passed: %5s, Skipped: %5s, Total: %5s, Projects: %s - dotnet test\n" \
    "$aggregate_status_label" "$aggregate_failed" "$aggregate_passed" "$aggregate_skipped" "$aggregate_total" "$aggregate_projects"
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
