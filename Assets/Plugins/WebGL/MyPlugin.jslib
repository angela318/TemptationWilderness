mergeInto(LibraryManager.library, {
  SceneLoaded: function () {
    if (typeof ReactUnityWebGL !== "undefined") {
      ReactUnityWebGL.SceneLoaded();
    }
  },
  ScrollCollected: function (count, total) {
    if (typeof ReactUnityWebGL !== "undefined") {
      ReactUnityWebGL.ScrollCollected(count, total);
    }
  },
  AllScrollsCollected: function () {
    if (typeof ReactUnityWebGL !== "undefined") {
      ReactUnityWebGL.AllScrollsCollected();
    }
  },
});
