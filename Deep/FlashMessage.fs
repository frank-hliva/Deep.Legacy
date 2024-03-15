namespace Deep

type FlashMessage = { Text : string; messageType : string }
type FlashMessageList = FlashMessage list

type FlashMessages(sessionManager : ISessionManager) =

    let falshMessagesKey = "flashMessages"

    member m.Send(text : string, ?messageType : string) = async {
            let messageType = defaultArg messageType "info"
            let item = [{ Text = text; messageType = messageType }]
            let! flashMessages = sessionManager.TryGetItem<FlashMessageList>(falshMessagesKey)
            let flashMessages =
                match flashMessages with
                | Some flashMessages -> flashMessages @ item
                | None -> item
            do! sessionManager.SetItem(falshMessagesKey, flashMessages)
        }

    member m.GetAll(?clear : bool) = async {
            let! flashMessages = sessionManager.TryGetItem<FlashMessageList>(falshMessagesKey)
            if defaultArg clear true then
                do! sessionManager.RemoveItem(falshMessagesKey)
            match flashMessages with
            | Some flashMessages -> return flashMessages
            | _ -> return List.empty
        }