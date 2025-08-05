# bletests cli

## direct running on gateway

- core devices
- core pair -a=C4:D3:6A:B4:82:F2 
- echo 472536 >> passkey.txt
- core trust -a=C4:D3:6A:B4:82:F2
- core binstate -a=C4:D3:6A:B4:82:F2
- core open -a=C4:D3:6A:B4:82:F2
- core unpair -a=C4:D3:6A:B4:82:F2

REMARK: As the pairing procedure requires entering a passkey and the cli does not allow to enter a 2nd command while the 1st command is not yet completed, wirte the 'passkey' to the file passkey.txt

## running with remote debugger on dev machine attached (on gateway)

As commands cannot be entered in visual studio, the cli listens to changes in the file 'commands.txt'

Establish an ssh session, navigate to the directory of the cli and use the following commands:

- echo core devices >> commands.txt
- echo core pair -a=C4:D3:6A:B4:82:F2 >> commands.txt
- echo 472536 >> passkey.txt
- echo core trust -a=C4:D3:6A:B4:82:F2 >> commands.txt
- echo core binstate -a=C4:D3:6A:B4:82:F2 >> commands.txt
- echo core open -a=C4:D3:6A:B4:82:F2 >> commands.txt
- echo core unpair -a=C4:D3:6A:B4:82:F2 >> commands.txt
