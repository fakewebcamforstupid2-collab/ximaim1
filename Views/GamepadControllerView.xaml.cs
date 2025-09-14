using System.Windows.Controls;
using System.Windows.Media;

namespace GamepadEmulator.Views
{
    public partial class GamepadControllerView : UserControl
    {
        public GamepadControllerView()
        {
            InitializeComponent();
        }

        public void UpdateLeftStick(short x, short y)
        {
            // Convert joystick values to visual position within the stick area
            var xPercent = (x / 32767.0) * 15; // 15 pixel range
            var yPercent = (-y / 32767.0) * 15; // Inverted Y
            
            Canvas.SetLeft(LeftStickIndicator, 135 + xPercent);
            Canvas.SetTop(LeftStickIndicator, 135 + yPercent);
        }

        public void UpdateRightStick(short x, short y)
        {
            var xPercent = (x / 32767.0) * 15;
            var yPercent = (-y / 32767.0) * 15;
            
            Canvas.SetLeft(RightStickIndicator, 255 + xPercent);
            Canvas.SetTop(RightStickIndicator, 175 + yPercent);
        }

        public void UpdateButton(string buttonName, bool pressed)
        {
            var brush = pressed ? new SolidColorBrush(Colors.White) : GetDefaultButtonBrush(buttonName);
            
            switch (buttonName.ToUpper())
            {
                case "A":
                    ButtonA.Fill = brush ?? new SolidColorBrush(Colors.Green);
                    break;
                case "B":
                    ButtonB.Fill = brush ?? new SolidColorBrush(Colors.Orange);
                    break;
                case "X":
                    ButtonX.Fill = brush ?? new SolidColorBrush(Colors.Blue);
                    break;
                case "Y":
                    ButtonY.Fill = brush ?? new SolidColorBrush(Colors.Yellow);
                    break;
                case "LEFTSHOULDER":
                    LeftShoulder.Fill = brush ?? new SolidColorBrush(Colors.Gray);
                    break;
                case "RIGHTSHOULDER":
                    RightShoulder.Fill = brush ?? new SolidColorBrush(Colors.Gray);
                    break;
                case "BACK":
                    BackButton.Fill = brush ?? new SolidColorBrush(Colors.DimGray);
                    break;
                case "START":
                    StartButton.Fill = brush ?? new SolidColorBrush(Colors.DimGray);
                    break;
            }
        }

        private Brush? GetDefaultButtonBrush(string buttonName)
        {
            return buttonName.ToUpper() switch
            {
                "A" => new SolidColorBrush(Colors.Green),
                "B" => new SolidColorBrush(Colors.Orange),
                "X" => new SolidColorBrush(Colors.Blue),
                "Y" => new SolidColorBrush(Colors.Yellow),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }
    }
}