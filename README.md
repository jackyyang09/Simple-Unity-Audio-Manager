# Jacky's Simple Audio Manager (JSAM)

Tired of having a billion AudioSources lying about? That will no longer be a reality!
Never worry about sounds and music ever again!

A quick, easy-to-use, decentralized audio playing system for Unity

Perfect for game jams, prototypes, or if you just want to get started on a project quickly

# Features
- Easily and intuitively add and play sounds and music!
- Control master volume, sound volume and music volume seperately and easily!
- Fade music in and out!
- Spatialized 3D sound and audio!

## Audio Feedback on

- Collision
- Trigger intersection
- Particle emit and death

## What's New?

Check out the [Releases](https://github.com/jackyyang09/Simple-Unity-Audio-Manager/releases) page to see all the latest updates!

If you'd like to see what parts of AudioManager I'm actively working on, you can check out the Trello [here!](https://trello.com/b/r6237lmD/audiomanager)

## How to Use
NOTE: Everything below here pertains to releases of AudioManager version 1.4.1 and below. As of AudioManager 2.0, the workflow listed below has been replaced with something completely different. Check out the [wiki](https://github.com/jackyyang09/Simple-Unity-Audio-Manager/wiki) and download the latest test build of version 2.0 in the Releases page if you'd like to ensure your project is using the latest supported build of AudioManager.

1. Download the latest package [here](https://github.com/jackyyang09/Simple-Unity-Audio-Manager/releases/download/1.4.1/JSAM.Audio.Manager.1.4.1.unitypackage) to get started!
2. Import the package into your Unity project
3. Everything begins with the AudioManager singleton!
4. Right click the hierarchy window, hover over the `Audio` label and add a new instance of the Audio Manager!
![Help1](https://github.com/jackyyang09/Simple-Unity-Audio-Manager/blob/Media/Media/audiomanager1.gif)
5. Click on `Add New Sound File` to begin populating your sound library. From there, rename your sound and drag+drop an AudioClip into the `File` field so that it's ready to use
![Help2](https://github.com/jackyyang09/Simple-Unity-Audio-Manager/blob/Media/Media/audiomanager2.gif)
6. When you're done adding sounds, go back to the Audio Manager and click `Re-Generate Audio Library` so Audio Manager registers all the new sounds
![Help3](https://github.com/jackyyang09/Simple-Unity-Audio-Manager/blob/Media/Media/audiomanager3.gif)
7. To play your newly added sound, you can add an `Audio Player` component to any object in the scene to have it play that sound automatically.
Alternatively, you can play sounds from anywhere in your scripts, you just have to type the following.
```
JSAM.AudioManager.instance.PlaySoundOnce("Shooting");
```
![Help4](https://github.com/jackyyang09/Simple-Unity-Audio-Manager/blob/Media/Media/audiomanager4.gif)

   - Be sure to replace "Shooting (But Better)" with the name of your sound!
   - Note: The name of the gameObject with the AudioFile component will be the string parameter used in AudioManager's dictionary!
   - You can also choose to pass in an audio clip rather than a string to play your sounds
   - Include `using JSAM` at the top of your script if you are planning to use AudioManager often!

Go ahead and check out the Example scenes and look at all the public functions within AudioManager to see what else you can do!
