import os

def extract_games(input_file):
    header_started = False
    game_lines = []
    game_headers = []
    i = 0
    for line in input_file:
        if line.startswith('['):
            if not header_started:
                if i > 0:
                    output_file = os.path.splitext(input_file.name)[0] + '_' + str(i) + '.txt'
                    i = i + 1
                    with open(output_file, 'w') as f:
                        f.writelines(game_headers)
                        f.writelines(game_lines)
                else:
                    i = 1
                game_lines = []
                game_headers = []
                game_headers.append(line)
                header_started = True
            else:
                game_headers.append(line)                
        else:
            header_started = False
            game_lines.append(line)

    # Last game that won't be written out in the loop above
    if not header_started:
        output_file = os.path.splitext(input_file.name)[0] + '_' + str(i) + '.txt'
        i = i + 1
        with open(output_file, 'w') as f:
            f.writelines(game_headers)
            f.writelines(game_lines)

with open('test.pgn', 'r') as input_file:
    extract_games(input_file)