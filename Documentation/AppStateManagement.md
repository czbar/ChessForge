# State Transitions

The application will need to switch between the Learning Modes as well as between States within those modes.

There are the following Learning Modes:
- Manual Review
- Training
- Engine Game

When switching modes, the GUI must be adjusted accordingly, appropriate controls must be made visible or hidden, relevant timers must be stopped or started.


## Switching between Learning Modes

When switching between Learning modes:

- Stop/complete engine evaluation if in progress.
- Stop all timers.
- Setup default controls visibility for the new mode 

## Switching states in the Manual Review mode

In the Manual Review mode, the application can be in one of the following states:
- Normal - the user is manually browsing moves in the Workbook view
- Move Evaluation - on user's request, evaluation of an individual move is running
- Line Evaluation - on user's request, evaluation of the current, "active" line is running
- Line Replay - on user's request, the active line is automatically replayed

When switching to Move or Line Evaluation:
- Line Replay must be stopped, if in progress
- Evaluation progress controls are made visible
- Evaluation Lines view replaces the Comment Box
- Timers required for engine evaluations are started.

When switching back from Move or Line Evaluation to Normal:
- Evaluation progress controls will be hidden
- Timers are stopped. (If the evaluation was stopped by the user forcibly, the engine evaluation must be stopped too.)
- Evaluation Lines view should remain visible until user takes some action (e.g. clicking somewhere) and then replaced with the Comment Box.


## Switching states within Training mode

In the Training mode, the application can be in one of the following states:
- Awaiting User Move
- Awaiting Workbook Move
- Move Evaluation
- Line Evaluation
- Idle

The training session starts in the Awaiting User Move state. When the user made their move, Training switches to Awaiting Workbook Move state. 
- Timer is started to pick up the Workbook move once the move is generated. The move is displayed and the timer stopped.

Once the Workbook move has been made, Training switches to the Awaiting User Move state. There are no changes in the GUI, unless the user's move was not in the Workbook and the applications switches from the Training learning mode to the Engine Game mode.

When switching to Move or Line Evaluation:
- Evaluation progress controls are made visible
- Evaluation Lines view replaces the Comment Box
- Timers required for engine evaluations are started.

When switching back from Move or Line Evaluation:
- Evaluation progress controls will be hidden
- Timers are stopped. (If the evaluation was stopped by the user forcibly, the engine evaluation must be stopped too.)
- Evaluation Lines view should remain visible until user takes some action (e.g. clicking somewhere) and then replaced with the Comment Box.

## Switching states within Engine Game mode

In the Engine Game mode, the aplication can be in one of the following states
- Awaiting User Move
- Awaiting Workbook Move

