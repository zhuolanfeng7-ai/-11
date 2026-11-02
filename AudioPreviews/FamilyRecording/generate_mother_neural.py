import asyncio
import sys
from pathlib import Path

root = Path(__file__).resolve().parent
sys.path.insert(0, str(root / '.voice_tools'))
import edge_tts

async def main():
    text = ("Why aren't you home yet? I've saved you some dinner... "
            "You have time to make money. Don't forget to take care of yourself. "
            "Call out when you get home, so I know you're back.")
    output = root / 'Mother_Message_Gentle_EN.mp3'
    voice = edge_tts.Communicate(text, voice='en-US-JennyNeural',
                                 rate='-12%', volume='-5%', pitch='-2Hz',
                                 connect_timeout=15, receive_timeout=30)
    await voice.save(str(output))
    print(f'{output}\n{output.stat().st_size} bytes')

asyncio.run(main())
