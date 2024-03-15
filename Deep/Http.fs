namespace Deep

open System
open System.Net
open System.Text
open System.IO
open System.Collections.Specialized
open System.Threading.Tasks
open System.Security.Cryptography.X509Certificates
open Deep.Routing
open System.Web

[<AutoOpen>]
module HttpExtensions =
    type WebHeaderCollection with
        member c.Add(newHeaders : string seq) =
            newHeaders |> Seq.iter(c.Add)

type Request internal (httpListenerRequest : HttpListenerRequest, parameters : RouteParams) =
    member this.GetClientCertificate() : X509Certificate2 = httpListenerRequest.GetClientCertificate()
    member this.BeginGetClientCertificate(requestCallback : AsyncCallback, state : Object) : IAsyncResult = httpListenerRequest.BeginGetClientCertificate(requestCallback, state)
    member this.EndGetClientCertificate(asyncResult : IAsyncResult) : X509Certificate2 = httpListenerRequest.EndGetClientCertificate(asyncResult)
    member this.GetClientCertificateAsync() : Task<X509Certificate2> = httpListenerRequest.GetClientCertificateAsync()
    member this.RequestTraceIdentifier with get() : Guid = httpListenerRequest.RequestTraceIdentifier
    member this.AcceptTypes with get() : String[] = httpListenerRequest.AcceptTypes
    member this.ContentEncoding with get() : Encoding = httpListenerRequest.ContentEncoding
    member this.ContentLength64 with get() : Int64 = httpListenerRequest.ContentLength64
    member this.ContentType with get() : String = httpListenerRequest.ContentType
    member this.Headers with get() : NameValueCollection = httpListenerRequest.Headers
    member this.HttpMethod with get() : String = httpListenerRequest.HttpMethod
    member this.InputStream with get() : Stream = httpListenerRequest.InputStream
    member this.IsAuthenticated with get() : Boolean = httpListenerRequest.IsAuthenticated
    member this.IsLocal with get() : Boolean = httpListenerRequest.IsLocal
    member this.IsSecureConnection with get() : Boolean = httpListenerRequest.IsSecureConnection
    member this.IsWebSocketRequest with get() : Boolean = httpListenerRequest.IsWebSocketRequest
    member this.QueryString with get() : NameValueCollection = HttpUtility.ParseQueryString(httpListenerRequest.Url.Query)
    member this.RawUrl with get() : String = httpListenerRequest.RawUrl
    member this.ServiceName with get() : String = httpListenerRequest.ServiceName
    member this.Url with get() : Uri = httpListenerRequest.Url
    member this.UrlReferrer with get() : Uri = httpListenerRequest.UrlReferrer
    member this.UserAgent with get() : String = httpListenerRequest.UserAgent
    member this.UserHostAddress with get() : String = httpListenerRequest.UserHostAddress
    member this.UserHostName with get() : String = httpListenerRequest.UserHostName
    member this.UserLanguages with get() : String[] = httpListenerRequest.UserLanguages
    member this.ClientCertificateError with get() : Int32 = httpListenerRequest.ClientCertificateError
    member this.TransportContext with get() : TransportContext = httpListenerRequest.TransportContext
    member this.Cookies with get() : CookieCollection = httpListenerRequest.Cookies
    member this.ProtocolVersion with get() : Version = httpListenerRequest.ProtocolVersion
    member this.HasEntityBody with get() : Boolean = httpListenerRequest.HasEntityBody
    member this.KeepAlive with get() : Boolean = httpListenerRequest.KeepAlive
    member this.RemoteEndPoint with get() : IPEndPoint = httpListenerRequest.RemoteEndPoint
    member this.LocalEndPoint with get() : IPEndPoint = httpListenerRequest.LocalEndPoint
    member this.Root = 
        let url = this.Url
        let port = if url.Port = 80 then "" else sprintf ":%d" url.Port
        sprintf "%s%s%s%s" url.Scheme Uri.SchemeDelimiter url.Host port
    member this.GetReader() : StreamReader = new StreamReader(httpListenerRequest.InputStream)
    member this.Params = parameters
    member this.GetRemoteClientInfo() = new RemoteClientInfo(this.RemoteEndPoint)
    new (httpListenerRequest) = Request(httpListenerRequest, Map.empty)

and RemoteClientInfo(remoteEndPoint : IPEndPoint) =
    let ipAddress = lazy remoteEndPoint.Address
    let lazyIPHostEntry = lazy (Dns.GetHostEntry(ipAddress.Value))
    member i.IPEndPoint = remoteEndPoint
    member i.IPAddress with get() = ipAddress.Value
    member i.IPHostEntry with get() = lazyIPHostEntry.Value
    member i.HostName with get() = lazyIPHostEntry.Value.HostName

type Response internal (httpListenerResponse : HttpListenerResponse, output : Output, defaultHeaders : string seq) =
    let headers =
        let headers = httpListenerResponse.Headers
        headers.Add("Server: \r\n\r\n")
        headers.Add("X-Powered-By: Deep")
        if defaultHeaders <> null
        then headers.Add(defaultHeaders)
        headers
    member this.CopyFrom(templateResponse : HttpListenerResponse) : unit = httpListenerResponse.CopyFrom(templateResponse)
    member this.AddHeader(name : String, value : String) : unit = httpListenerResponse.AddHeader(name, value)
    member this.AddHeaders(newHeaders : string seq) = headers.Add newHeaders
    member this.AppendHeader(name : String, value : String) : unit = httpListenerResponse.AppendHeader(name, value)
    member this.Redirect(url : String) : unit = httpListenerResponse.Redirect(url)
    member this.AppendCookie(cookie : Cookie) : unit = httpListenerResponse.AppendCookie(cookie)
    member this.SetCookie(cookie : Cookie) : unit = httpListenerResponse.SetCookie(cookie)
    member this.Abort() : unit = httpListenerResponse.Abort()
    member this.Close(responseEntity : Byte[], willBlock : Boolean) : unit = httpListenerResponse.Close(responseEntity, willBlock)
    member this.Close() : unit = httpListenerResponse.Close()
    member this.ContentEncoding with get() : Encoding = httpListenerResponse.ContentEncoding and set(value : Encoding) = httpListenerResponse.ContentEncoding <- value
    member this.ContentType with get() : String = httpListenerResponse.ContentType and set(value : String) = httpListenerResponse.ContentType <- value
    member this.OutputStream with get() : Stream = output.OutputStream
    member this.RawOutputStream with get() : Stream = httpListenerResponse.OutputStream
    member this.RedirectLocation with get() : String = httpListenerResponse.RedirectLocation and set(value : String) = httpListenerResponse.RedirectLocation <- value
    member this.StatusCode with get() : Int32 = httpListenerResponse.StatusCode and set(value : Int32) = httpListenerResponse.StatusCode <- value
    member this.StatusDescription with get() : String = httpListenerResponse.StatusDescription and set(value : String) = httpListenerResponse.StatusDescription <- value
    member this.Cookies with get() : CookieCollection = httpListenerResponse.Cookies and set(value : CookieCollection) = httpListenerResponse.Cookies <- value
    member this.SendChunked with get() : Boolean = httpListenerResponse.SendChunked and set(value : Boolean) = httpListenerResponse.SendChunked <- value
    member this.KeepAlive with get() : Boolean = httpListenerResponse.KeepAlive and set(value : Boolean) = httpListenerResponse.KeepAlive <- value
    member this.Headers with get() : WebHeaderCollection = httpListenerResponse.Headers and set(value : WebHeaderCollection) = httpListenerResponse.Headers <- value
    member this.ContentLength64 with get() : Int64 = httpListenerResponse.ContentLength64 and set(value : Int64) = httpListenerResponse.ContentLength64 <- value
    member this.ProtocolVersion with get() : Version = httpListenerResponse.ProtocolVersion and set(value : Version) = httpListenerResponse.ProtocolVersion <- value
    member this.GetWriter() : StreamWriter = new StreamWriter(this.OutputStream)