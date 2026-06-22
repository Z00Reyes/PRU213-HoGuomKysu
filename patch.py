import sys

def patch_scene(file_path):
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()

    # 1. Add FishingAudio and AudioSource components at the end of the file
    new_components = """--- !u!82 &8000001
AudioSource:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 471533512}
  m_Enabled: 1
  serializedVersion: 4
  OutputAudioMixerGroup: {fileID: 0}
  m_audioClip: {fileID: 0}
  m_Resource: {fileID: 0}
  m_PlayOnAwake: 1
  m_Volume: 1
  m_Pitch: 1
  Loop: 0
  Mute: 0
  Spatialize: 0
  SpatializePostEffects: 0
  Priority: 128
  DopplerLevel: 1
  MinDistance: 1
  MaxDistance: 500
  Pan2D: 0
  rolloffMode: 0
  BypassEffects: 0
  BypassListenerEffects: 0
  BypassReverbZones: 0
  rolloffCustomCurve:
    serializedVersion: 2
    m_Curve:
    - serializedVersion: 3
      time: 0
      value: 1
      inSlope: 0
      outSlope: 0
      tangentMode: 0
      weightedMode: 0
      inWeight: 0.33333334
      outWeight: 0.33333334
    - serializedVersion: 3
      time: 1
      value: 0
      inSlope: 0
      outSlope: 0
      tangentMode: 0
      weightedMode: 0
      inWeight: 0.33333334
      outWeight: 0.33333334
    m_PreInfinity: 2
    m_PostInfinity: 2
    m_RotationOrder: 4
  panLevelCustomCurve:
    serializedVersion: 2
    m_Curve:
    - serializedVersion: 3
      time: 0
      value: 0
      inSlope: 0
      outSlope: 0
      tangentMode: 0
      weightedMode: 0
      inWeight: 0.33333334
      outWeight: 0.33333334
    m_PreInfinity: 2
    m_PostInfinity: 2
    m_RotationOrder: 4
  spreadCustomCurve:
    serializedVersion: 2
    m_Curve:
    - serializedVersion: 3
      time: 0
      value: 0
      inSlope: 0
      outSlope: 0
      tangentMode: 0
      weightedMode: 0
      inWeight: 0.33333334
      outWeight: 0.33333334
    m_PreInfinity: 2
    m_PostInfinity: 2
    m_RotationOrder: 4
  reverbZoneMixCustomCurve:
    serializedVersion: 2
    m_Curve:
    - serializedVersion: 3
      time: 0
      value: 1
      inSlope: 0
      outSlope: 0
      tangentMode: 0
      weightedMode: 0
      inWeight: 0.33333334
      outWeight: 0.33333334
    m_PreInfinity: 2
    m_PostInfinity: 2
    m_RotationOrder: 4
--- !u!114 &8000002
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 471533512}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: d9a1c5ae7376af240ba5259536c9a140, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::FishingAudio
  castClip: {fileID: 0}
  reelClip: {fileID: 0}
"""
    if "--- !u!114 &8000002" not in content:
        content += new_components

    # 2. Add to MC component list
    comp_target = """  - component: {fileID: 471533516}
  - component: {fileID: 471533517}"""
    comp_replacement = """  - component: {fileID: 471533516}
  - component: {fileID: 471533517}
  - component: {fileID: 8000001}
  - component: {fileID: 8000002}"""
    if comp_replacement not in content:
        content = content.replace(comp_target, comp_replacement)

    # 3. Update PlayerController25D
    # Replace `fishingState: 0\n` with nothing if exists
    content = content.replace("  fishingState: 0\n", "")

    # Inject fishingAudioSource if not there
    pc_target = """  m_EditorClassIdentifier: Assembly-CSharp::PlayerController25D
  speed: 5"""
    pc_replacement = """  m_EditorClassIdentifier: Assembly-CSharp::PlayerController25D
  fishingAudioSource: {fileID: 8000002}
  speed: 5"""
    if pc_replacement not in content:
        content = content.replace(pc_target, pc_replacement)

    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(content)
        
    print("Patched successfully")

patch_scene('Assets/Scenes/MCScence.unity')
