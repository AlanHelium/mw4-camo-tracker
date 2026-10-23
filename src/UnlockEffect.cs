using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
namespace CamoTracker;
public partial class MainWindow
{
 Window? unlockToast;
 static DrawingImage TrophyImage(string color)
 {
  var drawing=new DrawingGroup();var brush=B(color);
  drawing.Children.Add(new GeometryDrawing(brush,null,Geometry.Parse("M10,5 L30,5 L28,20 Q26,27 21,28 L21,34 L29,34 L29,38 L11,38 L11,34 L19,34 L19,28 Q14,27 12,20 Z")));
  drawing.Children.Add(new GeometryDrawing(null,new Pen(brush,2.5),Geometry.Parse("M10,9 L4,9 L4,15 Q4,22 13,22 M30,9 L36,9 L36,15 Q36,22 27,22")));
  var image=new DrawingImage(drawing);image.Freeze();return image;
 }
 Border UnlockCard(Weapon w,Challenge c)
 {
  string color=c.Kind is "gold" or "platinum" or "harmonic"?"#E9D392":c.RequiredPrestige>0?"#C9B2F8":Acid;
  var layout=new Grid{Margin=new(17,12,17,12)};layout.ColumnDefinitions.Add(new(){Width=new GridLength(53)});layout.ColumnDefinitions.Add(new());
  var icon=new Border{Width=44,Height=52,CornerRadius=new(10),Background=B("#27332B"),Child=new Image{Source=TrophyImage(color),Width=28,Height=34,Margin=new(8)}};Put(layout,icon);
  var title=Label("迷彩已解锁 / CAMO UNLOCKED",9,color,true);var name=Label(Bilingual.Camo(c),15,White,true);name.TextWrapping=TextWrapping.NoWrap;name.TextTrimming=TextTrimming.CharacterEllipsis;name.Margin=new(0,4,0,2);var gun=Label(Bilingual.WeaponPair(w),10,Muted);gun.TextWrapping=TextWrapping.NoWrap;gun.TextTrimming=TextTrimming.CharacterEllipsis;var text=Stack(title,name,gun);text.VerticalAlignment=VerticalAlignment.Center;Put(layout,text,1);
  return new Border{Child=layout,Background=new LinearGradientBrush((Color)ColorConverter.ConvertFromString("#233029"),(Color)ColorConverter.ConvertFromString("#111917"),0),BorderBrush=B(color),BorderThickness=new(1,1,1,2),CornerRadius=new(12)};
 }
 void DismissUnlock(){unlockToast?.Close();unlockToast=null;}
 void ShowUnlockToast(Weapon w,Challenge c,bool force=false)
 {
  DismissUnlock();
  if(tracker.Data.UnlockSound)UnlockAudio.Play();
  if(!tracker.Data.UnlockEffects&&!force)return;
  var anchor=mini is {IsVisible:true}?mini:this;var card=UnlockCard(w,c);
  var toast=new Window{Width=370,Height=94,WindowStyle=WindowStyle.None,AllowsTransparency=true,Background=Brushes.Transparent,ResizeMode=ResizeMode.NoResize,Topmost=true,ShowInTaskbar=false,ShowActivated=false,Focusable=false,IsHitTestVisible=false,Content=card,FontFamily=FontFamily,Owner=anchor};
  toast.Left=Math.Clamp(anchor.Left+(anchor.ActualWidth-370)/2,0,Math.Max(0,SystemParameters.VirtualScreenWidth-370));toast.Top=Math.Max(SystemParameters.WorkArea.Top+8,anchor.Top-105);unlockToast=toast;
  toast.SourceInitialized+=(_,_)=>{var handle=new WindowInteropHelper(toast).Handle;SetWindowLongPtr(handle,-20,new IntPtr(GetWindowLongPtr(handle,-20).ToInt64()|0x20|0x8000000));};
  toast.Closed+=(_,_)=>{card.BeginAnimation(OpacityProperty,null);if(card.RenderTransform is TranslateTransform transform)transform.BeginAnimation(TranslateTransform.YProperty,null);if(unlockToast==toast)unlockToast=null;};toast.Show();
  if(!SystemParameters.ClientAreaAnimation)
  {
   var timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(2.4)};timer.Tick+=(_,_)=>{timer.Stop();toast.Close();};toast.Closed+=(_,_)=>timer.Stop();timer.Start();return;
  }
  var travel=new TranslateTransform(0,-10);card.RenderTransform=travel;
  var opacity=new DoubleAnimationUsingKeyFrames{Duration=TimeSpan.FromMilliseconds(2500)};
  opacity.KeyFrames.Add(new LinearDoubleKeyFrame(0,KeyTime.FromTimeSpan(TimeSpan.Zero)));
  opacity.KeyFrames.Add(new EasingDoubleKeyFrame(1,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200)),new CubicEase{EasingMode=EasingMode.EaseOut}));
  opacity.KeyFrames.Add(new LinearDoubleKeyFrame(1,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(2100))));
  opacity.KeyFrames.Add(new LinearDoubleKeyFrame(0,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(2500))));
  var move=new DoubleAnimation(-10,0,TimeSpan.FromMilliseconds(320)){EasingFunction=new BackEase{Amplitude=.2,EasingMode=EasingMode.EaseOut}};
  opacity.Completed+=(_,_)=>toast.Close();travel.BeginAnimation(TranslateTransform.YProperty,move);card.BeginAnimation(OpacityProperty,opacity);
 }
 void CaptureUnlockPreview(string path)
 {
  var card=UnlockCard(tracker.CurrentWeapon,tracker.Current);card.Measure(new Size(370,94));card.Arrange(new Rect(0,0,370,94));card.UpdateLayout();var bitmap=new RenderTargetBitmap(370,94,96,96,PixelFormats.Pbgra32);bitmap.Render(card);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var stream=File.Create(System.IO.Path.ChangeExtension(path,"unlock.png"));encoder.Save(stream);
 }
}
