using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Button _startCPUButton;
    [SerializeField] private Button _startGPUButton;
    [SerializeField] private TMP_InputField _particleCountInput;
    [SerializeField] private TMP_Text _particleSizeText;
    [SerializeField] private TMP_Text _dispersionAmountText;
    [SerializeField] private TMP_Text _smoothingLengthText;
    [SerializeField] private TMP_Text _restingDesnityText;
    [SerializeField] private TMP_Text _stiffnessConstantText;
    [SerializeField] private TMP_Text _viscosityCoefficientText;
    [SerializeField] private TMP_InputField _boxScaleXText;
    [SerializeField] private TMP_InputField _boxScaleYText;
    [SerializeField] private TMP_InputField _boxScaleZText;

    [SerializeField] private BoxCollider _cube;
    [SerializeField] private BoxCollider _tempCube;

    public ParticleSimulator CPUSim;
    public GPUParticleSimulator GPUSim;
    public GameObject CubeMarcher;

    public int particleCount;
    public bool showMesh;
    public bool showParticles;
    public float particleSize;
    public float dispersionAmount;
    public Vector3 startVelocity;
    public float smoothingLength;
    public float restingDensity; // Ideal density for the fluid
    public float stiffnessConstant;
    public float viscosityCoefficient;

    public float volume = 0.000000299f; // Volume of water molecule 2.99x10^-23
    public float molarMass = 0.018f; // Molar mass of water 18g -> 0.018kg

    [Header("Bounds")]
    public Vector3 boundsSize;
    public Vector3 boundsPosition;
    public float collisionDamping = 1;

    [Header("External Collision")]
    [SerializeField] private BoxCollider box;
    // Variables for the box (AABB)
    public Vector3 boxMin;
    public Vector3 boxMax;
    public float boundaryOffset;


    private bool _started = false;
    private bool _addCube = false;
    private bool _visualizeParticleColor = false;

    public void StartCPUSim()
    {
        particleCount = int.Parse(_particleCountInput.text);
        if (particleCount <= 0)
        {
            return;
        }

        ClearSim();

        CPUSim.particleCount = particleCount;
        CPUSim.dispersionAmount = dispersionAmount;

        CPUSim.StartSim();
        CubeMarcher.SetActive(true);
        CPUSim.gameObject.SetActive(true);
        _started = true;
    }

    public void StartGPUSim()
    {
        particleCount = int.Parse(_particleCountInput.text);
        if (particleCount <= 0)
        {
            return;
        }

        ClearSim();

        GPUSim.particleCount = particleCount;
        GPUSim.dispersionAmount = dispersionAmount;

        GPUSim.StartSim();
        GPUSim.gameObject.SetActive(true);
        _started = true;
    }


    public void ClearSim()
    {
        CPUSim.gameObject.SetActive(false);
        GPUSim.gameObject.SetActive(false);
        CubeMarcher.SetActive(false);
        _started = false;
    }

    public void SetShowMesh(bool value)
    {
        showMesh = value;
    }

    public void SetShowParticles(bool value)
    {
        showParticles = value;
    }

    public void SetParticleSize(float value)
    {
        particleSize = value;
        _particleSizeText.text = String.Format("{0:0.##}", value);
    }

    public void SetDispersionAmount(float value)
    {
        dispersionAmount = value;
        _dispersionAmountText.text = value.ToString();

    }

    public void SetSmoothingLength(float value)
    {
        smoothingLength = value;
        _smoothingLengthText.text = value.ToString();
    }

    public void SetRestDensity(float value)
    {
        restingDensity = value;
        _restingDesnityText.text = value.ToString();
    }

    public void SetStiffnessConstant(float value)
    {
        stiffnessConstant = value;
        _stiffnessConstantText.text = value.ToString();
    }

    public void SetViscosityCoefficient(float value)
    {
        viscosityCoefficient = value;
        _viscosityCoefficientText.text = String.Format("{0:0.##}", value);
    }

    public void SetAddCube(bool value)
    {
        _addCube = value;

        if (_addCube)
        {
            if (_tempCube != null)
            {
                return;
            }
            _tempCube = Instantiate(_cube, Vector3.zero, Quaternion.identity);
            _tempCube.gameObject.SetActive(true);
            CPUSim.box = _tempCube;
            GPUSim.box = _tempCube;
        }

        else
        {
            if (_tempCube == null)
            {
                return;
            }
            Destroy(_tempCube.gameObject);
            CPUSim.box = null;
            GPUSim.box = null;
        }
    }

    public void SetBoxScaleX()
    {
        if (_tempCube == null)
        {
            return;
        }
        float value = int.Parse(_boxScaleXText.text);
        _tempCube.transform.localScale = new Vector3(value, _tempCube.transform.localScale.y, _tempCube.transform.localScale.z);
    }

    public void SetBoxScaleY()
    {
        if (_tempCube == null)
        {
            return;
        }
        float value = int.Parse(_boxScaleYText.text);
        _tempCube.transform.localScale = new Vector3(_tempCube.transform.localScale.x, value, _tempCube.transform.localScale.z);
    }

    public void SetBoxScaleZ()
    {
        if (_tempCube == null)
        {
            return;
        }
        float value = int.Parse(_boxScaleZText.text);
        _tempCube.transform.localScale = new Vector3(_tempCube.transform.localScale.x, _tempCube.transform.localScale.y, value);
    }

    public void SetParticlesColor(bool value)
    {
        _visualizeParticleColor = value;
    }


    private void Update()
    {
        if(!_started) return;

        CubeMarcher.GetComponent<MeshRenderer>().enabled = showMesh;
        CPUSim.visualizeParticles = showParticles;
        GPUSim.visualizeParticles = showParticles;

        CPUSim.particleSize = particleSize;
        GPUSim.particleSize = particleSize;

        CPUSim.smoothingLength = smoothingLength;
        GPUSim.smoothingLength = smoothingLength;

        CPUSim.restingDensity = restingDensity;
        GPUSim.restingDensity = restingDensity;

        CPUSim.stiffnessConstant = stiffnessConstant;
        GPUSim.stiffnessConstant = stiffnessConstant;

        CPUSim.viscosityCoefficient = viscosityCoefficient;
        GPUSim.viscosityCoefficient = viscosityCoefficient;

        CPUSim.visualizeColorOnVelocity = _visualizeParticleColor;
        GPUSim.visualizeColorOnVelocity = _visualizeParticleColor;
    }
}
