module internal Deep.AutoDisposer

open System
open Deep

let disposeObjects(kernel : IKernel) =
    match kernel.TryFindInstance(typedefof<MultipartForm>) with
    | Some form ->
        let form = form :?> IAutoDisposable
        if not form.IsDisposed then form.Dispose()
    | _ -> ()
    match kernel.TryFindInstance(typedefof<Reply>) with
    | Some reply ->
        let reply = reply :?> IAutoDisposable
        if not reply.IsDisposed then reply.Dispose()
    | _ -> ()