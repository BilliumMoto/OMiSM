# OMtoSMConverter

Converts osu!mania files into stepmania files.
Drag and drop all osu!mania .osu files into the window, the converted files will show up in the same directory that this program is located in.

It can also copy hitsounds from one .osu osu!mania file to another, placing sample and volume information on notes where timestamps align and placing storyboard samples otherwise. 
Sample information is taken from source from a left-to-right priority, meaning the leftmost note will be copied before any to its right for any given timestamp. If the destination osu file has multiple notes with the same timestamp (a chord), one of these notes will be chosen randomly to recieve the hitsound.

See https://osu.ppy.sh/community/forums/topics/664319

Usage examples:
https://youtu.be/uf4-lr7V_Pw
https://youtu.be/WLqrKwlZrUg
