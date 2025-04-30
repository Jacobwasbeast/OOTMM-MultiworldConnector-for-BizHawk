# OOTMM MultiworldConnector for BizHawk

A lightweight external tool that allows seamless communication with the Ocarina of Time & Majora's Mask (OOTMM) Multiworld client via the BizHawk emulator. This connector enables sending and receiving items and events in real time as part of a multiworld game.

## Features

- Integrates directly with BizHawk using its ExternalTools system
- Allows sending and receiving data through the OOTMM multiworld client
- Simple installation — just drop in a single .dll

## Requirements

- [BizHawk Emulator 2.10](https://tasvideos.org/BizHawk/ReleaseHistory#Bizhawk210)
- [OOTMM Multiworld Client](https://ootmm.com/multiplayer) running and configured 

## Installation

1. Download the `MultiworldConnector.dll`.
2. Place it into the following folder inside your BizHawk directory: BizHawk/ExternalTools/
3. Launch BizHawk and you should see it inside the Tools-External Tool menu 
4  Click on 'MultiWorld Connector' and trust it
5. Press connect and the tool should automatically load and begin communicating with the multiworld client.
6. Keep the window open so it keeps it connection.

## Usage

Once installed and loaded:

- The connector will interface with the OOTMM client automatically.
- No additional configuration needed inside BizHawk.
- Make sure the multiworld client is running and connected to your session.

## Notes

- This is a simple external tool primarily designed for Ocarina of Time and Majora's Mask Combo Multiworld randomizer.
- The tool operates passively; it will not interfere with standard BizHawk operation or other tools.

## Troubleshooting

- If the tool does not appear to be working:
- Ensure the DLL is placed in the correct folder.
- Check that the OOTMM client is running and connected.
- Restart BizHawk after placing the DLL if it was already open.

## License

MIT License
