using Godot;
using System;
using System.Threading.Tasks;

public partial class Explosion : Node3D
{
    [Export] public GpuParticles3D debris;
    [Export] public GpuParticles3D fire;
    [Export] public GpuParticles3D dust;
    [Export] public AudioStreamPlayer3D audio;
    [Export] public Camera3D cam;

    public async void Explode() 
    {
        cam.Current = true;
        debris.Emitting = true;
        fire.Emitting = true;
        dust.Emitting = true;
        
        if (audio != null) audio.Play();
    }

    private void OnMitsubishiDead(Vector3 position)
    {
        GlobalPosition = position;
        Explode();
    }
}