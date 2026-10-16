mergeInto(LibraryManager.library, {
  GameOver: function (score) {
    ReactUnityWebGL.GameOver(score);
  },
  BreathingStatus: function (active) {
    ReactUnityWebGL.BreathingStatus(active);
  },
  FishCollected: function () {
    ReactUnityWebGL.FishCollected();
  },
});