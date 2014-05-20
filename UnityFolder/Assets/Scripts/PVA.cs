﻿using UnityEngine;
using System.Collections;

public class PVA : MonoBehaviour 
{	
	public bool rotationPointsToCurrentVelocity = false;

	public Vector3 position;
	public Vector3 velocity;
	public Vector3 acceleration;

	public Vector3 rotationalVelocity;
	public Vector3 rotationalAcceleration;

	public float zRotationVelocity;
	public float zRotationAcceleration;

	[Range(0,0.1f)]
	public float velocityDecay = 0;

	[Range(0,1)]
	public float accelerationDecay = 0;

	public Space refrenceFrame = Space.World;
	Vector3 deltaPos;

	public bool isDecay = false;
	public float velocityKillThreashold = 0.0f;
	public Vector3 deltaV;
	Vector3 previousV;

	public float timeStep = 1.0f/1000.0f;
	public float stepCounter = 0;
	public int loopCounter = 0;

	// Use this for initialization
	void Start () 
	{
		Init();
		//previousV = new Vector3(0, 0, 0);
	}

	void Init()
	{
		position = transform.position;
	}
	
	// Update is called once per frame
	void Update () 
	{

		float currentDT = Time.deltaTime;

		loopCounter = 0;
		while(stepCounter < currentDT)
		{
			CalculatePVA();
			stepCounter += timeStep;
			loopCounter ++;
		}
		stepCounter -= currentDT;
			
		transform.Translate(deltaPos, refrenceFrame);
		deltaPos = Vector3.zero;

	}

	private void CalculatePVA()
	{
		// do core PVA update

		velocity += acceleration * timeStep;
		deltaPos += (velocity + previousV) * 0.5f * timeStep;

		rotationalVelocity += rotationalAcceleration * timeStep;
		
		if(isDecay)
		{
			// apply decay
			velocity -= velocityDecay * velocity * timeStep;
			acceleration -= -accelerationDecay * acceleration * timeStep;

			rotationalVelocity -= velocityDecay * rotationalVelocity * timeStep;
			rotationalAcceleration -= accelerationDecay * rotationalAcceleration * timeStep;

		}

		if( Mathf.Abs(velocity.x) <= velocityKillThreashold )
			velocity.x = 0;
		if( Mathf.Abs(velocity.y) <= velocityKillThreashold )
			velocity.y = 0;
		if( Mathf.Abs(velocity.z) <= velocityKillThreashold )
			velocity.z = 0;
		if( Mathf.Abs(zRotationVelocity) <= velocityKillThreashold )
			zRotationVelocity = 0;

		deltaV = velocity - previousV;
		previousV = velocity;


	}

	public void ResetPVA()
	{
		Init();
	}


}
