# About
This script is used to create a Lua file on Pastebin that can be used to control an organ in modded Minecraft using the steam pipes from the Create Mod.
The pipes are controlled using a series of "server" computers from ComputerCraft, which toggle a note to play for a specified number of seconds based on a "controller" computer.
One server is used for all 39 possible notes (13 per pipe size, ranging from F#3-F#4, F#4-F#5, and F#5-F#6), and three sub-controllers are used for each pipe size (Bottom, Middle, or Top, roughly equivalent to the three organ manuals Choir, Great, and Swell).

The script clamps all notes in the MIDI file to be within the playable range (F#3-F#6), then divides the notes across the three sets of pipes.
For ties (F#4 and F#5), if the note is approached from below, the tie is broken using the F# in the same pipe set; otherwise the set above is chosen.

Once the notes are divided into their respective set of pipes, a Lua script is generated for each sub-controller with the notes respective to its controlled set.
Finally, the master controller is generated a Lua script to download the respective scripts onto each sub-controller, wait 10 seconds for them to finish downloading, then play the piece of music.

A video demonstration can be seen [here]().

# Usage
See https://github.com/danielathome19/MIDIToComputerCraft/releases for a downloadable demo along with the Minecraft world save (made in the FTB Direwolf20 1.18.0 modpack).

You will need to make a Pastebin account to obtain an API key, which the program will prompt for along with the username/password if a **.env** file is not found in the current program directory.

# Bugs/Features
Bugs are tracked using the GitHub Issue Tracker.

Please use the issue tracker for the following purpose:
  * To raise a bug request; do include specific details and label it appropriately.
  * To suggest any improvements in existing features.
  * To suggest new features or structures or applications.

# License
The code is licensed under Apache License 2.0.
