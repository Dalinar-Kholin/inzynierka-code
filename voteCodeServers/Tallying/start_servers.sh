#!/bin/bash

MAX_SERVERS=10
SESSION_NAME="vote-servers"

if ! command -v tmux &> /dev/null; then
    echo "Error: tmux not found. Install it with: sudo dnf install tmux"
    exit 1
fi

tmux kill-session -t $SESSION_NAME 2>/dev/null

echo "Starting $MAX_SERVERS servers in tmux session '$SESSION_NAME'..."

# Utwórz pierwszy panel z serwerem 1
tmux new-session -d -s $SESSION_NAME "dotnet run 1 $MAX_SERVERS"

# Dodaj kolejne panele pionowo (serwery 2-10)
for i in $(seq 2 $MAX_SERVERS); do
    tmux split-window -t $SESSION_NAME -v "dotnet run $i $MAX_SERVERS"
    tmux select-layout -t $SESSION_NAME tiled
done

tmux select-layout -t $SESSION_NAME tiled

# Ustaw fokus na ostatni panel (serwer 10)
tmux select-pane -t $SESSION_NAME:.$((MAX_SERVERS-1))

echo "All $MAX_SERVERS servers started in tmux session"
echo ""
echo "Commands:"
echo "  Attach to session:     tmux attach -t $SESSION_NAME"
echo "  Switch between panes:  Ctrl+b then arrow keys"
echo "  Next pane:             Ctrl+b o"
echo "  Show pane numbers:     Ctrl+b q (then press number)"
echo "  Zoom pane:             Ctrl+b z (toggle fullscreen)"
echo "  Scroll mode:           Ctrl+b [ (q to exit)"
echo "  Detach:                Ctrl+b d"
echo "  Kill all:              tmux kill-session -t $SESSION_NAME"
echo ""

tmux attach -t $SESSION_NAME