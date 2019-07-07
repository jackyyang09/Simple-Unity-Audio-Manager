# Simple-Unity-Audio-Manager

Never worry about sounds and music ever again!

A quick, easy-to-use, decentralized audio playing system for Unity

Perfect for game jams, prototypes and if you just want to get started on a project quickly

# Features
- Easily and intuitively add and play sounds and music!
- Control master volume, sound volume and music volume seperately and easily!
- Fade music in and out!

## Audio Feedback on

- Collision
- Trigger intersection
- Particle emit and death

# How to Use

1. Download the latest package [here](https://github.com/jackyyang09/Simple-Unity-Audio-Manager/releases)
2. Import the package into your Unity project
3. Check out the Examples folder for a sample scene showing all the ways you can use AudioManager!
4. Everything begins with the AudioManager singleton!

![Cat](https://raw.githubusercontent.com/jackyyang09/Simple-Unity-Audio-Manager/Media/Media/media1.png)

5. Drag and drop the AudioManager prefab into your scene
6. Add sounds to AudioManager's audio libraries

![Cat](https://raw.githubusercontent.com/jackyyang09/Simple-Unity-Audio-Manager/Media/Media/media2.png)

7. To play your newly added sound, go into any script file and type the following
```
AudioManager.GetInstance().PlaySoundOnce("Shooting");
```
   - Be sure to replace "Shooting" with the name of your sound!
   - You can also choose to pass in an audio clip rather than a string to play your sounds

Go ahead and look at all the public functions within AudioManager to see what else you can do!
