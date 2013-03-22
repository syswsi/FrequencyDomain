using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class PlayerScript : MonoBehaviour 
{
	public float hControlSpeed = 1.0f;
	public float vControlSpeed = 1.0f;

	public float newHeight = 0;

	public Vector3 velocity = new Vector3();
	Vector3 oldVelocity = new Vector3();
	public float friction = 0.0f;
	public float gravity = 0.0f;
	public float rampUpFactor = 1.0f;
	public float rampUpCounter = 0;

	public float[] boostFactorArray = new float[3];
	public float[] boostTreasholdArray = new float[2];
	public int boostStage = 0;
	public float energyCounter = 0;
	public float originalFieldOfView = 90;
	public float currentFieldOfView;

	public float orignalBloomIntensityValue = 2.0f;
	public float originalLightsRange = 250.0f;

	float bloomBurstValue;

	public float bloomBurstScale = 1.0f;
	public float meshLightsScale = 1.0f;

	public float bloomBurstMinimumHeight = 10;

	public float jumpHeight;
	public float hangtimeCounter;
	public float[] bloomBurstValueArray = new float[5];

	public float hangTimeScale = 1;

	public int activeCoroutineCounter = 0;

	MeshFieldGeneratorScript meshFieldGeneratorScript;
	Camera mainCamera;
	Bloom bloomScript;
	List<Light> meshLightsList = new List<Light>();

	// Use this for initialization
	void Start () 
	{
		meshFieldGeneratorScript = (MeshFieldGeneratorScript)GameObject.Find("MainMeshField").GetComponent("MeshFieldGeneratorScript");
		mainCamera = (Camera)GameObject.Find("Main Camera").GetComponent("Camera");
		bloomScript = (Bloom)GameObject.Find("Main Camera").GetComponent("Bloom");
		GameObject[] meshLightsObjectsArray =  GameObject.FindGameObjectsWithTag("MeshLight");
		
		for(int i =0; i < meshLightsObjectsArray.Length; i++)
		{
			meshLightsList.Add( (Light)(meshLightsObjectsArray[i]).GetComponent("Light") );
		}


	}
	
	// Update is called once per frame
	void Update () 
	{
		Vector3 oldPosition = transform.position;
		oldVelocity = velocity;

		float xTranslation = Input.GetAxis("Horizontal") * hControlSpeed;
		float yTranslation = Input.GetAxis("Vertical") * vControlSpeed;
		/*
		// cancel velocity in axis if changing direction (left/right only)
		if( xTranslation * velocity.z < 0) // if directions are oppsite
			velocity.z = 0;
	*/
		// apply new force to velocity
		velocity += new Vector3( -yTranslation, 0 , xTranslation);

		// only apply friction to translation, not gravity/height
		velocity.x -= velocity.x * friction;
		velocity.z -= velocity.z * friction;

		//Get New Height
		newHeight = meshFieldGeneratorScript.getHeightFromPosition(transform.position.x -1, transform.position.z);
		
		if( oldPosition.y < newHeight) // ramping up
		{
			rampUpCounter += (newHeight - oldPosition.y) * Time.deltaTime ; // keep track of of much height is gained
			velocity.y = 0;
			oldPosition.y = newHeight; // hug mesh
		}
		else if( oldPosition.y > newHeight) // flying in the air
		{
			if( rampUpCounter > 0 ) // the first moment in the air
			{
				jumpHeight = transform.position.y;
				velocity.y += rampUpCounter * rampUpFactor; // apply velocity gained from ramp
				//Debug.Log(velocity.y);
				rampUpCounter = 0; // reset it
			}
			else // in free fall
			{
				velocity.y -= gravity * Time.deltaTime; // apply gravity 
				hangtimeCounter += Time.deltaTime;
				//Debug.Log(gravity * Time.deltaTime);
			}
		}

		// if oldPosition.y and newHeight are equal, oldPosition stays untouched.
		transform.position = oldPosition + velocity * Time.deltaTime;

		HandleBoost();
		HandleBloomBurst();
		HandleMeshLights();
	}

	void HandleBoost()
	{
		currentFieldOfView = mainCamera.fieldOfView;

		if( currentFieldOfView < boostTreasholdArray[0] )
			boostStage = 0;
		else if( currentFieldOfView < boostTreasholdArray[1] )
			boostStage = 1;
		else
			boostStage = 2;

		float boostFactor = 0;
		boostFactor = boostFactorArray[boostStage];

		if( Input.GetButton("Fire1") || Input.GetButton("Jump") )
		{
			energyCounter += Time.deltaTime * boostFactor;
		}
		else
		{
			energyCounter -= Time.deltaTime * boostFactor;
			if(energyCounter < 0)
				energyCounter = 0;
		}

		float rgbValue =  energyCounter/(181 - originalFieldOfView) ;

		if( originalFieldOfView + energyCounter < 180 )
		{
			mainCamera.fieldOfView = originalFieldOfView + energyCounter;
			mainCamera.backgroundColor = new Color(rgbValue,rgbValue,rgbValue,rgbValue);
		}


	}

	void HandleBloomBurst()
	{
		if(oldVelocity.y < 0 && velocity.y == 0 && jumpHeight > bloomBurstMinimumHeight) // moment of impact with ground
		{				
			//bloomBurstValue = -oldVelocity.y * bloomBurstScale;
			bloomBurstValue = hangtimeCounter * bloomBurstScale;
			activeCoroutineCounter++;
			StartCoroutine(BloomBurstDegradeCoroutine(hangtimeCounter, bloomBurstValue));
			hangtimeCounter = 0;
		}
		float bloomBurstSum = 0;
		for(int i = 0; i < bloomBurstValueArray.Length; i++)
			bloomBurstSum += bloomBurstValueArray[i];
		bloomScript.bloomIntensity = orignalBloomIntensityValue + bloomBurstSum;

	}

	IEnumerator BloomBurstDegradeCoroutine(float hangTime, float initialBloomBurst)
	{	
		for(int i =0; i < bloomBurstValueArray.Length; i++)
		{
			// find an empty stack slot
			if(bloomBurstValueArray[i] == 0)
			{
				float timeCounter = 0;
				hangTime = hangTime * hangTimeScale;
				bloomBurstValueArray[i] = initialBloomBurst;
				while( timeCounter < hangTime )
				{
					bloomBurstValueArray[i] = Mathf.Lerp(bloomBurstValueArray[i], 0, timeCounter/hangTime);
					timeCounter += Time.deltaTime;
					if(bloomBurstValueArray[i] < 0.00001 ) // kill the co-routine if value is too low to be noticeable (this seems to fix inconsistencies bug)
						break;
					yield return null;
				}
				bloomBurstValueArray[i] = 0;
				activeCoroutineCounter --;
				break;
			}
		}
	}

	void HandleMeshLights()
	{
		float bloomBurstSum = 0;
		for(int i = 0; i < bloomBurstValueArray.Length; i++)
			bloomBurstSum += bloomBurstValueArray[i]/bloomBurstScale; //undo bloombusrt scale


		for(int i = 0; i < meshLightsList.Count; i++ )
			meshLightsList[i].range = originalLightsRange + bloomBurstSum * meshLightsScale;
	}


}
