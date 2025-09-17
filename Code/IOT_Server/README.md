# IOT Server Application

This is a separate TCP server application that receives distance reports from Unity anchors and performs trilateration to estimate tag positions.

## How to Run

### 1. Start the Server Application

Navigate to the IOT_Server directory and compile/run the C# program:

**Option A: Using .NET SDK (if available):**
```bash
cd /Users/spino/Desktop/university/IOT_Server
dotnet run Program.cs
```

**Option B: Using Visual Studio or MonoDevelop:**
1. Open Program.cs in Visual Studio or MonoDevelop
2. Build and run the project

**Option C: Using Mono (if available on macOS):**
```bash
cd /Users/spino/Desktop/university/IOT_Server
mcs Program.cs -out:IOTServer.exe
mono IOTServer.exe
```

**Option D: Copy to Unity and run as MonoBehaviour (Alternative):**
If you can't compile the standalone C# program, you can adapt the server code to run within Unity itself.

The server will start listening on port 5000 for incoming TCP connections.

### 2. Run the Unity Client Application

1. Open the main IOT Unity project in Unity Editor
2. Make sure the scene contains:
   - Tags (objects with Tag.cs script)
   - Anchors (objects with Anchor.cs script and "Anchor" tag)
   - MessageBus (object with MessageBus.cs script)

3. Run the Unity application

## What Happens

1. **Unity Side**: Tags initiate TWR (Two-Way Ranging) procedures with anchors
2. **Unity Side**: Anchors calculate distances and send reports via TCP to the server
3. **Server Side**: Server receives distance reports and performs trilateration
4. **Server Side**: Estimated positions are written to files in the Results directory

## Data Format

The anchors send data to the server in CSV format:
```
tagName,pollId,anchorName,anchorPosX,anchorPosY,anchorPosZ,distance
```

## Results

Position estimates are saved to:
- **Server**: `IOT_Server/Results/{tagName}.txt`
- Format: `x,y,z` coordinates

## Changes Made

- **Anchor.cs**: Modified to send distance reports via TCP instead of local Server instance
- **Server.cs**: Commented out (functionality moved to separate TCP server)
- **MessageBus.cs**: Fixed merge conflicts
- **TCP Server**: Standalone C# application handles trilateration and file output

## Configuration

- Server IP: `127.0.0.1` (localhost)
- Server Port: `5000`
- These can be changed in `Anchor.cs` constants if needed
