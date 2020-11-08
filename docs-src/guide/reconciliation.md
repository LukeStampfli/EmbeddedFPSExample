# Reconciliation

Reconciliation is the process of correcting the client's predicted position. In real environments reconciliation is probably the hardest thing to get done correctly. Because if a player has a very bad connection his predictions will almost always be wrong which will make that player jitter around and creates ruber band effects. Some games increase the size of both the client and the server buffer in that case to fix that. It's not a bad approach but will add a huge delay on players with bad connections which means their server position will be behind what they actually see by a lot.

We will just do basic reconciliation with the expectation that the server performs 1 client input per frame.

So how is reconciliation done?
- We store all inputs and positions and their respective frame numbers that we predicted in a list (called a historyBuffer or just history).
- When we receive a server message we receive a last processed input value (the uint Frame in the GameUpdateData).
- We can then delete all entries in the history which have a smaller number then the one we received.
- Next we compare the received position to the stored predicted position. If it differs enough we will set the players position to the server position.
- But now we've set the player to a position in the past so we have to fix that. We do that by iteration through the rest of the entries and applying all inputs again.

So open the ClientPlayer script and create a new Struct above the ClientPlayer class:
```csharp
struct ReconciliationInfo
{
    public ReconciliationInfo(uint frame, PlayerUpdateData data, PlayerInputData input)
    {
        Frame = frame;
        Data = data;
        Input = input;
    }

    public uint Frame;
    public PlayerUpdateData Data;
    public PlayerInputData Input;
}
```

Then in the ClientPlayer class create a history to store our predicted information:
```csharp
private Queue<ReconciliationInfo> reconciliationHistory = new Queue<ReconciliationInfo>();
```

in FixedUpdate after:
```csharp
using (Message message = Message.Create((ushort) Tags.GamePlayerInput, inputData))
{
    ConnectionManager.Instance.Client.SendMessage(message, SendMode.Reliable);
}
```
add the input and position to the history:
```csharp
    reconciliationHistory.Enqueue(new ReconciliationInfo(GameManager.Instance.ClientTick, nextStateData, inputData));
```

Now let's implement the reconciliation. We will do that in the if(IsOwn) brackets of the OnServerUpdate function of the ClientPlayer. Fill it with the following lines:
```csharp
while (reconciliationHistory.Any() && reconciliationHistory.Peek().Frame < GameManager.Instance.LastReceivedServerTick)
{
    reconciliationHistory.Dequeue();
}

if (reconciliationHistory.Any() && reconciliationHistory.Peek().Frame == GameManager.Instance.LastReceivedServerTick)
{
    ReconciliationInfo info = reconciliationHistory.Dequeue();
    if (Vector3.Distance(info.Data.Position, playerStateData.Position) > 0.05f)
    {

        List<ReconciliationInfo> infos = reconciliationHistory.ToList();
        interpolation.CurrentData = playerStateData;
        transform.position = playerStateData.Position;
        transform.rotation = playerStateData.LookDirection;
        for (int i = 0; i < infos.Count; i++)
        {
            PlayerStateData u = playerLogic.GetNextFrameData(infos[i].Input, interpolation.CurrentData);
            interpolation.SetFramePosition(u);
        }
    }
}
```

This is just basic reconciliation as explained at the beginning of this section. We correct the player if his position is off more then 0.05f. It's possible to choose a much bigger value for that something like 1f or 2f to allow players with bad connections to play with less corrections but that will cause rubber banding which is also terrible to play with. The best way to support players with bad connections is still to use bigger buffers.

This is already everything we need for reconciliation it should work now you can test it by stopping the server and moving a player by hand and resume it, then the client will also make a jump.

In the next section we will implement shooting, health and lag compensation for shots.