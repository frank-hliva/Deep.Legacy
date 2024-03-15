namespace Deep.Imaging.Encoding

open System.IO
open System.Windows.Media.Imaging
open Media.WebP

type encoder = Stream -> BitmapSource -> unit

type Encoder = 

    static member Use (encoder : BitmapEncoder) : encoder =
        fun (stream : Stream) (source : BitmapSource)  ->
            encoder.Frames.Add(BitmapFrame.Create(source))
            encoder.Save(stream)
            stream.Flush()

    /// <summary>
    /// Encodes the given RGB(A) bitmap to the given stream. Specify quality = -1 for lossless, otherwise specify a value between 0 and 100.
    /// </summary>
    static member UseWebP (quality : float32) : encoder =
        fun (stream : Stream) (source : BitmapSource)  ->
            WebPEncoder.Encode(source, stream, quality)
            WebPEncoder.Encode(source, stream, quality)