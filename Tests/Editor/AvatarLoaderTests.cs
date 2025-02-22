using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ReadyPlayerMe.AvatarLoader.Tests
{
    public class AvatarLoaderTests
    {
        [TearDown]
        public void Cleanup()
        {
            TestUtils.DeleteAvatarDirectoryIfExists(TestAvatarData.DefaultAvatarUri.Guid, true);
            TestUtils.DeleteAvatarDirectoryIfExists(TestUtils.TEST_WRONG_GUID, true);
        }

        [UnityTest]
        public IEnumerator AvatarLoader_Complete_Load()
        {
            GameObject avatar = null;
            var avatarUrl = string.Empty;
            var failureType = FailureType.None;

            var loader = new AvatarObjectLoader();
            loader.OnCompleted += (sender, args) =>
            {
                avatar = args.Avatar;
                avatarUrl = args.Url;
            };
            loader.OnFailed += (sender, args) => { failureType = args.Type; };
            loader.LoadAvatar(TestAvatarData.DefaultAvatarUri.ModelUrl);

            yield return new WaitUntil(() => avatar != null || failureType != FailureType.None);

            Assert.AreEqual(TestAvatarData.DefaultAvatarUri.ModelUrl, avatarUrl);
            Assert.AreEqual(FailureType.None, failureType);
            Assert.IsNotNull(avatar);
            Assert.IsNotNull(avatar.GetComponent<AvatarData>());
        }

        [UnityTest]
        public IEnumerator AvatarLoader_Fail_Load()
        {
            var failureType = FailureType.None;
            var avatarUrl = string.Empty;

            var loader = new AvatarObjectLoader();
            loader.OnFailed += (sender, args) =>
            {
                failureType = args.Type;
                avatarUrl = args.Url;
            };
            loader.LoadAvatar(TestAvatarData.WrongUri.ModelUrl);

            yield return new WaitUntil(() => failureType != FailureType.None);

            Assert.AreEqual(TestAvatarData.WrongUri.ModelUrl, avatarUrl);
            Assert.AreNotEqual(FailureType.None, failureType);
        }

        [UnityTest]
        public IEnumerator AvatarLoader_Replace_Old_Avatar_Instance()
        {
            GameObject avatarA = null;
            GameObject avatarB = null;
            var failureType = FailureType.None;

            var loaderA = new AvatarObjectLoader();
            loaderA.OnCompleted += (_, args) => avatarA = args.Avatar;
            loaderA.OnFailed += (_, args) => failureType = args.Type;
            loaderA.LoadAvatar(TestAvatarData.DefaultAvatarUri.ModelUrl);

            yield return new WaitUntil(() => avatarA != null || failureType != FailureType.None);

            var loaderB = new AvatarObjectLoader();
            loaderB.OnCompleted += (_, args) => avatarB = args.Avatar;
            loaderB.OnFailed += (_, args) => failureType = args.Type;
            loaderB.LoadAvatar(TestAvatarData.DefaultAvatarUri.ModelUrl);

            yield return new WaitUntil(() => avatarB != null || failureType != FailureType.None);

            Animator[] objects = Object.FindObjectsOfType<Animator>();

            Assert.AreEqual(1, objects.Length);
            Assert.AreEqual(FailureType.None, failureType);
        }

        [UnityTest]
        public IEnumerator AvatarLoader_Clears_Persistent_Cache()
        {
            AvatarLoaderSettings settings = AvatarLoaderSettings.LoadSettings();
            settings.AvatarCachingEnabled = true;

            GameObject avatarA = null;
            var failureType = FailureType.None;

            var loader = new AvatarObjectLoader();
            loader.OnCompleted += (_, args) => avatarA = args.Avatar;
            loader.OnFailed += (_, args) => failureType = args.Type;
            loader.LoadAvatar(TestAvatarData.DefaultAvatarUri.ModelUrl);

            yield return new WaitUntil(() => avatarA != null || failureType != FailureType.None);

            Assert.AreEqual(FailureType.None, failureType);
            Assert.AreEqual(false, AvatarCache.IsCacheEmpty());

            AvatarCache.Clear();
            Assert.AreEqual(true, AvatarCache.IsCacheEmpty());

            settings.AvatarCachingEnabled = false;
        }

        [UnityTest]
        public IEnumerator AvatarLoader_Cancel_Loading()
        {
            GameObject avatar = null;
            var failureType = FailureType.None;
            var loader = new AvatarObjectLoader();

            loader.OnCompleted += (sender, args) =>
            {
                avatar = args.Avatar;
            };
            loader.OnFailed += (sender, args) => { failureType = args.Type; };
            loader.LoadAvatar(TestAvatarData.DefaultAvatarUri.ModelUrl);

            var frameCount = 0;
            const int cancelAfterFramesCount = 10;

            while (failureType == FailureType.None && avatar == null)
            {
                if (frameCount > cancelAfterFramesCount)
                {
                    loader.Cancel();
                }

                frameCount++;
                yield return null;
            }

            Assert.AreNotEqual(FailureType.None, failureType);
            Assert.AreEqual(null, avatar);
        }
    }
}
