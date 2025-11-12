(scriptParam, timeParam) => {
  scriptSteps: scriptParam ?? [];
  timeValue: timeParam ?? 0;

  clamp: (valueParam, minParam, maxParam) => {
    value: valueParam ?? 0;
    minValue: minParam ?? 0;
    maxValue: maxParam ?? 1;
    eval if (value < minValue)
      then minValue
      else if (value > maxValue)
        then maxValue
        else value;
  };

  safeCursor: (cursorParam, textParam) => {
    textValue: textParam ?? '';
    cursorValue: cursorParam ?? 0;
    textLengthValue: length(textValue);
    eval clamp(cursorValue, 0, textLengthValue);
  };

  takePrefix: (textParam, countParam) => {
    textValue: textParam ?? '';
    countValue: math.max(countParam ?? 0, 0);
    eval substring(textValue, 0, countValue);
  };

  insertText: (stateParam, segmentParam) => {
    state: stateParam ?? {};
    textValue: state.text ?? '';
    cursorValue: safeCursor(state.cursor ?? 0, textValue);
    textLengthValue: length(textValue);
    before: substring(textValue, 0, cursorValue);
    after: substring(textValue, cursorValue, textLengthValue);
    insertValue: segmentParam ?? '';
    eval {
      text: before + insertValue + after;
      cursor: cursorValue + length(insertValue);
      ready: state.ready ?? 0;
      remainingTime: state.remainingTime ?? 0;
      done: state.done ?? 0;
    };
  };

  deleteForward: (stateParam, countParam) => {
    state: stateParam ?? {};
    textValue: state.text ?? '';
    cursorValue: safeCursor(state.cursor ?? 0, textValue);
    textLengthValue: length(textValue);
    deleteCountValue: math.max(countParam ?? 0, 0);
    boundedCount: clamp(deleteCountValue, 0, textLengthValue - cursorValue);
    before: substring(textValue, 0, cursorValue);
    after: substring(textValue, cursorValue + boundedCount, textLengthValue);
    eval {
      text: before + after;
      cursor: cursorValue;
      ready: state.ready ?? 0;
      remainingTime: state.remainingTime ?? 0;
      done: state.done ?? 0;
    };
  };

  deleteBackward: (stateParam, countParam) => {
    state: stateParam ?? {};
    textValue: state.text ?? '';
    cursorValue: safeCursor(state.cursor ?? 0, textValue);
    deleteCountValue: math.max(countParam ?? 0, 0);
    boundedCount: clamp(deleteCountValue, 0, cursorValue);
    startIndex: cursorValue - boundedCount;
    before: substring(textValue, 0, startIndex);
    after: substring(textValue, cursorValue, length(textValue));
    eval {
      text: before + after;
      cursor: startIndex;
      ready: state.ready ?? 0;
      remainingTime: state.remainingTime ?? 0;
      done: state.done ?? 0;
    };
  };

  deleteText: (stateParam, stepParam, appliedCountParam) => {
    step: stepParam ?? {};
    directionValue: step.direction ?? 'backward';
    appliedCount: math.max(appliedCountParam ?? 0, 0);
    eval if (appliedCount <= 0)
      then stateParam
      else if (directionValue = 'forward')
        then deleteForward(stateParam, appliedCount)
        else deleteBackward(stateParam, appliedCount);
  };

  applyReadyFlag: (stateParam, markReadyParam, completedParam) => {
    markValue: markReadyParam ?? 0;
    completedFlag: completedParam ?? 0;
    readyValue: stateParam.ready ?? 0;
    eval if (completedFlag = 0)
      then readyValue
      else if (markValue > 0)
        then 1
        else readyValue;
  };

  processStep: (stateParam, stepParam) => {
    state: stateParam ?? {};
    step: stepParam ?? {};
    remainingTime: state.remainingTime ?? 0;
    doneFlag: state.done ?? 0;
    eval if (doneFlag = 1)
      then state
      else {
        kind: step.kind ?? 'wait';
        eval if (kind = 'type')
          then {
            textSegment: step.text ?? '';
            speedValue: math.max(step.speed ?? 0, 0);
            charCountValue: length(textSegment);
            durationValue:
              if (speedValue <= 0)
                then 0
                else charCountValue / speedValue;
            eval if (charCountValue = 0)
              then state
              else if (durationValue <= 0)
                then {
                  typedState: insertText(state, textSegment);
                  eval {
                    text: typedState.text;
                    cursor: typedState.cursor;
                    ready: applyReadyFlag(typedState, step.markReady ?? 0, 1);
                    remainingTime;
                    done: 0;
                  };
                }
              else if (remainingTime >= durationValue)
                then {
                  typedState: insertText(state, textSegment);
                  eval {
                    text: typedState.text;
                    cursor: typedState.cursor;
                    ready: applyReadyFlag(typedState, step.markReady ?? 0, 1);
                    remainingTime: remainingTime - durationValue;
                    done: 0;
                  };
                }
              else {
                partialCountRaw: math.floor(remainingTime * speedValue);
                partialCount: clamp(partialCountRaw, 0, charCountValue);
                partialText: takePrefix(textSegment, partialCount);
                partialState: insertText(state, partialText);
                eval {
                  text: partialState.text;
                  cursor: partialState.cursor;
                  ready: applyReadyFlag(partialState, step.markReady ?? 0, 0);
                  remainingTime: 0;
                  done: 1;
                };
              };
          }
          else if (kind = 'delete')
            then {
              deleteCountValue: math.max(step.count ?? 0, 0);
              speedValue: math.max(step.speed ?? 0, 0);
              durationValue:
                if (speedValue <= 0)
                  then 0
                  else deleteCountValue / speedValue;
              eval if (deleteCountValue = 0)
                then state
                else if (durationValue <= 0)
                  then deleteText(state, step, deleteCountValue)
                else if (remainingTime >= durationValue)
                  then {
                    deletedState: deleteText(state, step, deleteCountValue);
                    eval {
                      text: deletedState.text;
                      cursor: deletedState.cursor;
                      ready: deletedState.ready ?? state.ready ?? 0;
                      remainingTime: remainingTime - durationValue;
                      done: 0;
                    };
                  }
                else {
                  partialCountRaw: math.floor(remainingTime * speedValue);
                  partialCount: clamp(partialCountRaw, 0, deleteCountValue);
                  partialState: deleteText(state, step, partialCount);
                  eval {
                    text: partialState.text;
                    cursor: partialState.cursor;
                    ready: partialState.ready ?? state.ready ?? 0;
                    remainingTime: 0;
                    done: 1;
                  };
                };
            }
            else if (kind = 'moveCursor')
              then {
                durationValue: math.max(step.duration ?? 0, 0);
                textValue: state.text ?? '';
                targetKind: step.position ?? 'end';
                targetCursor:
                  if (targetKind = 'start')
                    then 0
                    else if (targetKind = 'end')
                      then length(textValue)
                      else targetKind;
                clampedTarget: clamp(targetCursor ?? 0, 0, length(textValue));
                eval if (durationValue <= 0)
                  then {
                    cursorValue: clampedTarget;
                    eval {
                      text: textValue;
                      cursor: cursorValue;
                      ready: state.ready ?? 0;
                      remainingTime;
                      done: 0;
                    };
                  }
                  else if (remainingTime >= durationValue)
                    then {
                      cursorValue: clampedTarget;
                      eval {
                        text: textValue;
                        cursor: cursorValue;
                        ready: state.ready ?? 0;
                        remainingTime: remainingTime - durationValue;
                        done: 0;
                      };
                    }
                  else {
                    eval {
                      text: textValue;
                      cursor: state.cursor ?? 0;
                      ready: state.ready ?? 0;
                      remainingTime: 0;
                      done: 1;
                    };
                  };
              }
              else {
                durationValue: math.max(step.duration ?? 0, 0);
                eval if (durationValue <= 0)
                  then state
                  else if (remainingTime >= durationValue)
                    then {
                      eval {
                        text: state.text ?? '';
                        cursor: state.cursor ?? 0;
                        ready: state.ready ?? 0;
                        remainingTime: remainingTime - durationValue;
                        done: 0;
                      };
                    }
                    else {
                      eval {
                        text: state.text ?? '';
                        cursor: state.cursor ?? 0;
                        ready: state.ready ?? 0;
                        remainingTime: 0;
                        done: 1;
                      };
                    };
              };
      };
  };

  initialState: {
    text: '';
    cursor: 0;
    ready: 0;
    remainingTime: math.max(timeValue, 0);
    done: 0;
  };

  scriptResult:
    scriptSteps reduce (acc, step) => processStep(acc, step) ~ initialState;

  finalText: scriptResult.text ?? '';
  finalCursorRaw: scriptResult.cursor ?? 0;
  finalCursor: clamp(math.floor(finalCursorRaw), 0, length(finalText));
  finalReady: clamp(scriptResult.ready ?? 0, 0, 1);

  eval {
    text: finalText;
    cursorIndex: finalCursor;
    readyFlag: finalReady;
  };
};
