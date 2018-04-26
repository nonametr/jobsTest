using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public enum TaskType { CALLBACK, TERRAIN_PIECE_UPDATE };
public abstract class TaskBase
{
    public TaskBase(TaskType type) { this.type = type; }
    public TaskType type;
    public abstract void execute();
}

public class TerrainPieceFinishHMUpdateTask : TaskBase
{
    private GPU_TerrainMeshGenerator terrain;
    private TerrainPiece terrainPiece;
    
    public TerrainPieceFinishHMUpdateTask(GPU_TerrainMeshGenerator terrain, TerrainPiece terrainPiece) : base(TaskType.TERRAIN_PIECE_UPDATE)
    {
        this.terrain = terrain;
        this.terrainPiece = terrainPiece;
    }
    
    public override void execute()
    {
        terrain.HMRT.toTexture2D(terrainPiece.nhm_texture);
        terrain.SplatMapDiffuseRT.toTexture2D(terrainPiece.diffuse_texture);
        terrain.SplatMapNormalRT.toTexture2D(terrainPiece.normal_texture);
    }
}

public class TerrainPieceUpdateTask : TaskBase
{
    private GPU_TerrainMeshGenerator terrain;
    private TerrainPiece terrainPiece;
    private float xStart;
    private float yStart;
    private float xEnd;
    private float yEnd;
    
    public TerrainPieceUpdateTask(GPU_TerrainMeshGenerator terrain, TerrainPiece terrainPiece, float xStart, float yStart, float xEnd, float yEnd) : base(TaskType.TERRAIN_PIECE_UPDATE)
    {
        this.terrain = terrain;
        this.terrainPiece = terrainPiece;
        this.xStart = xStart;
        this.xEnd = xEnd;
        this.yStart = yStart;
        this.yEnd = yEnd;
    }
    
    public override void execute()
    {
        terrain.HMRT.material.SetVector("_RenderArea", new Vector4(xStart, yStart, xEnd, yEnd));
        terrain.SplatMapControlRT.material.SetVector("_RenderArea", new Vector4(xStart, yStart, xEnd, yEnd));
        terrain.HMRT.Update();
        terrain.SplatMapControlRT.Update();
        terrain.SplatMapDiffuseRT.Update();
        terrain.SplatMapNormalRT.Update();
        TaskManager.inst.priorityTaskQueue.Enqueue(new TerrainPieceFinishHMUpdateTask(terrain, terrainPiece));
    }
}

public class CallbackTask : TaskBase
{
    System.Action callback;
    public CallbackTask(System.Action callback) : base(TaskType.CALLBACK)
    {
        this.callback = callback;
    }
    public override void execute()
    {
        callback();
    }
}

[ExecuteInEditMode]
public class TaskManager : MonoBehaviour {
    public ConcurrentQueue<TaskBase> priorityTaskQueue = new ConcurrentQueue<TaskBase>();
    public ConcurrentQueue<TaskBase> taskQueue = new ConcurrentQueue<TaskBase>();

    public static TaskManager _inst;
    public static TaskManager inst
    {
        get
        {
            if (_inst == null)
            {
                _inst = GameObject.FindObjectOfType<TaskManager>();
            }

            return _inst;
        }
    }
    // Update is called once per frame
    void Update ()
    {
        TaskBase task;
        if (priorityTaskQueue.Count > 0)
        {
            priorityTaskQueue.TryDequeue(out task);
            if (task != null)
            {
                task.execute();
            }
        }
        if (taskQueue.Count > 0)
        {
            taskQueue.TryDequeue(out task);
            if (task != null)
            {
                task.execute();
            }
        }
	}
}
