#!/bin/bash

# Skrypt do uruchamiania 10 serwerów VoteCodesGenerator
# Używa tmux do stworzenia osobnych paneli dla każdego serwera
# Użycie: ./start_servers.sh

MAX_SERVERS=10
SESSION_NAME="vote-servers"

# Sprawdź czy tmux jest dostępny
if ! command -v tmux &> /dev/null; then
    echo "Error: tmux not found. Install it with: sudo dnf install tmux"
    exit 1
fi

# Zabij starą sesję jeśli istnieje
tmux kill-session -t $SESSION_NAME 2>/dev/null

echo "Starting $MAX_SERVERS servers in tmux session '$SESSION_NAME'..."

# Utwórz nową sesję tmux z serwerem 2 (serwer 1 dodamy na końcu, żeby był ostatnim panelem)
tmux new-session -d -s $SESSION_NAME "dotnet run 2 $MAX_SERVERS"

# Format ramek pokazuje numer panela i jego tytuł (tytuł aktualizuje aplikacja)
tmux set-option -t $SESSION_NAME pane-border-status top
# Pokazuj tylko tytuł panela ustawiany przez aplikację (bez numerów tmux)
tmux set-option -t $SESSION_NAME pane-border-format " #{pane_title} "
tmux set-option -t $SESSION_NAME allow-rename on

# Podziel okno na panele dla pozostałych serwerów (2x5 grid)
for i in $(seq 3 $MAX_SERVERS); do
    if [ $i -le 5 ]; then
        tmux split-window -t $SESSION_NAME -h "dotnet run $i $MAX_SERVERS"
        tmux select-layout -t $SESSION_NAME tiled
    else
        tmux split-window -t $SESSION_NAME -v "dotnet run $i $MAX_SERVERS"
        tmux select-layout -t $SESSION_NAME tiled
    fi
done

# Dodaj serwer 1 jako ostatni, żeby był domyślnie wybrany
tmux split-window -t $SESSION_NAME -v "dotnet run 1 $MAX_SERVERS"
tmux select-layout -t $SESSION_NAME tiled

# Ustaw layout na równomiernie rozmieszczone panele
tmux select-layout -t $SESSION_NAME tiled

# Ustaw fokus na panel serwera 1 (ostatni utworzony pane, index 9)
tmux select-pane -t $SESSION_NAME:.9

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

# Automatycznie podłącz się do sesji
tmux attach -t $SESSION_NAME
