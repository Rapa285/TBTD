Bootstrap should load first out of any monos, and will exist in a special Scene called bootstrap which will be loaded first.

the Bootstrap scene will contain important static game objects such as (but not limited to)
1. FMOD Bankloader*
2. MusicPlayer*
3. TimeService
4. LastJumpTracker
5. DataPersistenceManager
6. ConfigWorker (not yet implemented)

another important job that Bootstrap will do is make sure ConfigWorker reads config and aplies it first before loading the next scene (defined in Bootstrap)

