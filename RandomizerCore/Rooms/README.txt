Custom palace rooms

Put .json, or .yaml room files directly here in Rooms/, then select Custom
under Palaces → Room Pool. (The app refreshes the files before generation.)

A file can contain one room or a list of rooms. Use a unique name across all
rooms, including bundled ones. Rooms in this folder default to the CUSTOM
group. Set group explicitly to use another group. CustomRoomPool.yaml
selects rooms by group, tag, or name. To include all enabled CUSTOM rooms,
add CUSTOM to groups_include.

Each definition needs name, author, palaceNumber, connections, sideviewData
and enemies.

use palaceNumber: null for a room shared across palaces 1–6, or specify
a number from 1 to 7 for a specific palace.

Set priority: 1 to force a room to be picked before regular priority 0 rooms.
Rooms still need to fit the slot and pass the pool rules. You can add this to
a single room, or to the entire CUSTOM group in CustomRoomPool.yaml like this:

groups:
- name: CUSTOM
  priority: 1

Save one of the two examples because they describe the same room.

JSON example

{
  "name": "Example Room",
  "author": "Your name",
  "palaceNumber": null,
  "tags": ["Local"],
  "bitmask": "0B",
  "connections": "FC00FC00",
  "sideviewData": "27600F08F75010F1D2071082138F1305D9136071D409A021D20B7102D10EE30082C29080A00F08",
  "enemies": "0739445E8B7C44",
  "elevatorScreen": 0
}

YAML example

name: Example Room
author: Your name
palaceNumber: null
tags: [Local]
itembits: 0b1011
connections: 0b0101
sideviewData: "27600F08F75010F1D2071082138F1305D9136071D409A021D20B7102D10EE30082C29080A00F08"
enemies: "0739445E8B7C44"
elevatorScreen: 0


Requirements examples

Add the requirements field to your room definition. If a room has no
requirements, omit the field or use an empty list.

At the outer level of requirements, each entry is an alternative (OR).
A single ability is one alternative. A nested list is also one alternative,
but every ability in that nested list is required (AND). The same rules
apply inside the requires field of a targeted requirement.

Either of two abilities

Separate entries are alternatives. This example accepts GLOVE or DOWNSTAB.

JSON
  "requirements": ["GLOVE", "DOWNSTAB"]

YAML
  requirements: [GLOVE, DOWNSTAB]

One ability or a pair of abilities

FAIRY satisfies this requirement by itself. Without FAIRY, both GLOVE and
DOWNSTAB are required.

JSON
  "requirements": ["FAIRY", ["GLOVE", "DOWNSTAB"]]

YAML
  requirements:
    - FAIRY
    - [GLOVE, DOWNSTAB]

Both abilities to collect a key (roomJamGTM 7)

The room uses target KEY because the requirement applies to collecting the
key. Put GLOVE and DOWNSTAB in the same inner list to require both. The outer
list under requires holds the alternatives, which in this case is one pair.

JSON
  "requirements": [
    {
      "target": "KEY",
      "requires": [["GLOVE", "DOWNSTAB"]]
    }
  ]

YAML
  requirements:
    - target: KEY
      requires:
        - [GLOVE, DOWNSTAB]

A key for particular exits (Vanilla P2 M22)

With target EXIT, a route can specify the entering direction (from), the
exiting direction (to), or both. This room requires KEY when entering from
the west or exiting to the west. The route entries are alternatives (OR).
When one route includes both from and to, both directions must match (AND).

JSON
  "requirements": [
    {
      "target": "EXIT",
      "requires": ["KEY"],
      "routes": [
        {"to": ["WEST"]},
        {"from": ["WEST"]}
      ]
    }
  ]

YAML
  requirements:
    - target: EXIT
      requires: [KEY]
      routes:
        - to: [WEST]
        - from: [WEST]


Room format reference
https://github.com/Ellendar/Z2Randomizer/wiki/Rooms-File-Format
