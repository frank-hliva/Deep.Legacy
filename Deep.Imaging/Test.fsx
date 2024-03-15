#r @"c:\Pluton\Deep\Deep\bin\Release\Deep.dll"
#r @"System.ComponentModel.DataAnnotations"
#r @"System.Numerics"
#r @"System.Transactions.dll"
#r @"WindowsBase.dll"
#r @"PresentationCore.dll"
#r @"PresentationFramework.dll"
#r @"System.Xaml.dll"
#r @"System.Drawing"
#r @"c:\Pluton\WebP.NET\WebP.NET\bin\Release\WebP.NET.dll"

#load "Imaging.Sizing.fs"
#load "Imaging.Encoding.fs"
#load "Imaging.fs"
#load "Imaging.Video.fs"

open Deep.Imaging
open Deep.Imaging.Encoding
open System.Windows.Media.Imaging
open System.IO
open System.Net
open System

open Media.WebP

//let fe = @"c:\Pluton\PlutonCore\PlutonCoreApp\Web\img\alpha-blue.webp" |> File.ReadAllBytes |> WebPFeatures.From

let img = Image.From(@"c:\Pluton\original.tif")

let qualities = seq { for i in 1 .. 10 do yield i * 10 }

//let webPFeatures = WebPFeatures.Create(@"c:\Pluton\test080.webp" |> File.ReadAllBytes)

qualities
|> Seq.iter
    (fun q ->
        //let encoder = new JpegBitmapEncoder()
        //encoder.QualityLevel <- q
        img.Save(sprintf @"c:\Pluton\test%s.webp" (q.ToString("000")), Encoder.UseWebP(float32 q))
    )

