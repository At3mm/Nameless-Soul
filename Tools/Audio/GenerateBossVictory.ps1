param([string]$OutputDirectory = 'Assets/Effects/Audio/BossVictory')
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
Add-Type -TypeDefinition @'
using System;
using System.IO;
public static class BossVictorySynth {
 const int Rate=44100;
 static double Tone(double hz,double t) {return Math.Sin(2*Math.PI*hz*t);}
 static double Bell(double hz,double t) {if(t<0)return 0;return (Tone(hz,t)+.32*Tone(hz*2.01,t)+.12*Tone(hz*3.98,t))*Math.Exp(-t*2.1)*(1-Math.Exp(-t*70));}
 static void Save(string file,double seconds,int kind) {
  int n=(int)(Rate*seconds);var samples=new double[n];var random=new Random(4719);double peak=0,noise=0;
  for(int i=0;i<n;i++) {double t=(double)i/Rate,v=0;
   if(kind==0) {noise=.92*noise+.08*(random.NextDouble()*2-1);v=.72*Tone(51,t)*Math.Exp(-t*2.3)+.4*Tone(78+.5*Math.Sin(t*5),t)*Math.Exp(-t*2)+.22*Bell(147,t)+.32*noise*Math.Exp(-t*9);v*=1-Math.Exp(-t*260);}
   else if(kind==1) {double[] hz={146.83,220,293.66,349.23,440,587.33};for(int k=0;k<hz.Length;k++){double q=t-k*.075;v+=.12*Bell(hz[k],q);}v+=.15*Tone(73.415,t)*Math.Exp(-t*1.1)*(1-Math.Exp(-t*7));v=Math.Round(v*512)/512;}
   else {v=.38*Bell(587.33,t)+.25*Bell(880,t-.095)+.2*Bell(1174.66,t-.19);}
   v*=Math.Min(1,(seconds-t)/.18);samples[i]=v;peak=Math.Max(peak,Math.Abs(v));
  }
  using(var w=new BinaryWriter(File.Create(file))){w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+n*2);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(Rate);w.Write(Rate*2);w.Write((short)2);w.Write((short)16);w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(n*2);foreach(double s in samples)w.Write((short)(s/Math.Max(.001,peak)*26000));}
 }
 public static void Generate(string folder){Save(Path.Combine(folder,"BossDefeatImpact.wav"),2.4,0);Save(Path.Combine(folder,"BossVictoryResolve.wav"),3.8,1);Save(Path.Combine(folder,"BossRewardChime.wav"),1.6,2);}
}
'@
[BossVictorySynth]::Generate([System.IO.Path]::GetFullPath($OutputDirectory))
Get-ChildItem -LiteralPath $OutputDirectory -Filter '*.wav' | Select-Object Name,Length
