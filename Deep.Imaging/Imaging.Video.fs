module Deep.Imaging.Video

open System
open System.Windows.Media
open System.Windows.Media.Imaging
open System.Windows
open System.IO
open System.IO.Packaging

let private createTempFile(stream : Stream) = async {
    let path = @"e:/xxxx.mp4"
    let fileStream = File.OpenWrite(path)

    let buffer = Array.create(16 * 1024) 0uy
    let rec loop () = async {
        let! read = stream.AsyncRead(buffer, 0, buffer.Length)
        if read > 0 then
            let! _ = fileStream.AsyncWrite(buffer, 0, read) |> Async.Catch
            do! loop() }
    do! loop()
    fileStream.Flush()
    fileStream.Close()
    fileStream.Dispose()
    return path }

let createPreview(waitTime : int) (position : int) (stream : Stream) = async {
    let player = new MediaPlayer(Volume = 0.0, ScrubbingEnabled = true)
    let! uri = stream |> createTempFile
    player.Open(uri |> Uri)
    player.Pause()
    player.Position <- TimeSpan.FromSeconds(float position)
    System.Threading.Thread.Sleep(waitTime * 1000)
    let rtb = new RenderTargetBitmap(120, 90, 96.0, 96.0, PixelFormats.Pbgra32)
    let drawingVisual = new DrawingVisual()
    using
        (drawingVisual.RenderOpen())
        (_.DrawVideo(player, new Rect(0.0, 0.0, 120.0, 90.0)))
    rtb.Render(drawingVisual)
    let frame = BitmapFrame.Create(rtb).GetCurrentValueAsFrozen() :?> BitmapFrame
    let encoder = new JpegBitmapEncoder()
    encoder.Frames.Add(frame)
    let memoryStream = new MemoryStream()
    encoder.Save(memoryStream)
    player.Close()
    return memoryStream.ToArray() }