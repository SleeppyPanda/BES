import re

# Let's inspect Player Prefab and Scripts
player_motor = r"c:\Users\Admin\Documents\BES\Assets\_Project\Scripts\Gameplay\PlayerMotor.cs"
with open(player_motor, 'r', encoding='utf-8') as f:
    print("--- PlayerMotor.cs snippet ---")
    lines = f.readlines()
    for l in lines[:60]:
        print(l, end='')
