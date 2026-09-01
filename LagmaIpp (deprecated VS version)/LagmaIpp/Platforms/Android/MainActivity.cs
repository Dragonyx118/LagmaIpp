using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using LagmaIpp.Views;

namespace LagmaIpp
{
    [Activity(
        Theme = "@style/Maui.SplashTheme",
        MainLauncher = true,
        LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges =
            ConfigChanges.ScreenSize | ConfigChanges.Orientation |
            ConfigChanges.UiMode | ConfigChanges.ScreenLayout |
            ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        // ── Controller Bluetooth / USB ────────────────────────────

        public override bool OnGenericMotionEvent(MotionEvent? e)
        {
            if (e != null &&
                (e.Source & InputSourceType.Gamepad) == InputSourceType.Gamepad)
            {
                GamepadBar.FeedAndroidMotion(e);
                return true;
            }
            return base.OnGenericMotionEvent(e);
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent? e)
        {
            if (e != null &&
                (e.Source & InputSourceType.Gamepad) == InputSourceType.Gamepad)
            {
                GamepadBar.FeedAndroidKey(keyCode, pressed: true);
                return true;
            }
            return base.OnKeyDown(keyCode, e);
        }

        public override bool OnKeyUp(Keycode keyCode, KeyEvent? e)
        {
            if (e != null &&
                (e.Source & InputSourceType.Gamepad) == InputSourceType.Gamepad)
            {
                GamepadBar.FeedAndroidKey(keyCode, pressed: false);
                return true;
            }
            return base.OnKeyUp(keyCode, e);
        }
    }
}
