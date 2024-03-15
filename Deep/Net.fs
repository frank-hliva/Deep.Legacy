module Deep.Net

open System.Net
open System.Net.Sockets

type UdpClient with

    member c.SendAsync (bytes: byte[]) =
        Async.FromBeginEnd((fun (asyncCallback, s) -> c.BeginSend(bytes, bytes.Length, asyncCallback, s)), c.EndSend)

    member c.ReceiveAsync (endPoint: IPEndPoint ref) =
        Async.FromBeginEnd(c.BeginReceive, fun asyncCallback -> c.EndReceive(asyncCallback, endPoint)) 