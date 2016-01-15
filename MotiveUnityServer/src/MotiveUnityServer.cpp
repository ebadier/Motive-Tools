//=============================================================================
// Copyright © NaturalPoint, Inc. All Rights Reserved.
// 
// This software is provided by the copyright holders and contributors "as is" and
// any express or implied warranties, including, but not limited to, the implied
// warranties of merchantability and fitness for a particular purpose are disclaimed.
// In no event shall NaturalPoint, Inc. or contributors be liable for any direct,
// indirect, incidental, special, exemplary, or consequential damages
// (including, but not limited to, procurement of substitute goods or services;
// loss of use, data, or profits; or business interruption) however caused
// and on any theory of liability, whether in contract, strict liability,
// or tort (including negligence or otherwise) arising in any way out of
// the use of this software, even if advised of the possibility of such damage.
//=============================================================================


/*

UnitySample.cpp

This program connects to a NatNet server, receives a data stream, encodes a skeleton to XML, and
outputs XML locally over UDP to Unity.  The purpose is to illustrate how to get data into Unity3D.

Usage [optional]:

	UnitySample [ServerIP] [LocalIP] [Unity3D IP]

	[ServerIP]			IP address of the server (e.g. 192.168.0.107) ( defaults to local machine)
*/

#include <stdio.h>
#include <tchar.h>
#include <conio.h>
#include <winsock2.h>
#include <string>
#include <sstream>
#include <map>

#include "NatNetTypes.h"
#include "NatNetClient.h"

#include "tinyxml/tinyxml.h"  //== for xml encoding of Unity3D payload
#include "NatNetRepeater.h"   //== for transport of data over UDP to Unity3D

//== Slip Stream globals ==--

cSlipStream *gSlipStream;
std::map<int, std::string> gBoneNames;

#pragma warning( disable : 4996 )

void __cdecl DataHandler(sFrameOfMocapData* data, void* pUserData);		// receives data from the server
void __cdecl MessageHandler(int msgType, char* msg);		            // receives NatNet error mesages
void resetClient();
int CreateClient(int iConnectionType);

unsigned int MyServersDataPort = 3130;
unsigned int MyServersCommandPort = 3131;

NatNetClient* theClient;
FILE* fp;

char szMyIPAddress[128] = "";
char szServerIPAddress[128] = "";
char szUnityIPAddress[128] = "";

void SendXMLToUnity(sFrameOfMocapData *data, void* pUserData);

int _tmain(int argc, _TCHAR* argv[])
{
    int iResult;
    int iConnectionType = ConnectionType_Multicast;
    //int iConnectionType = ConnectionType_Unicast;
    
    // parse command line args
    if(argc>1)
    {
        strcpy(szServerIPAddress, argv[1]);	// specified on command line
        printf("Connecting to server at %s...\n", szServerIPAddress);
    }
    else
    {
        strcpy(szServerIPAddress, "");		// not specified - assume server is local machine
        printf("Connecting to server at LocalMachine\n");
    }
    if(argc>2)
    {
        strcpy(szMyIPAddress, argv[2]);	    // specified on command line
        printf("Connecting from %s...\n", szMyIPAddress);
    }
    else
    {
        strcpy(szMyIPAddress, "");          // not specified - assume server is local machine
        printf("Connecting from LocalMachine...\n");
    }
    if(argc>3)
    {
        strcpy(szUnityIPAddress, argv[3]);	    // specified on command line
        printf("Connecting to Unity3D at %s...\n", szUnityIPAddress);
    }
    else
    {
        strcpy(szUnityIPAddress, "127.0.0.1");          // not specified - assume server is local machine
        printf("Connecting to Unity3D on LocalMachine...\n");
    }
	if(argc>4)
	{
		iConnectionType = strcmp(argv[4], "unicast") != 0 ? ConnectionType_Multicast : ConnectionType_Unicast;
	}

    gSlipStream = new cSlipStream(szUnityIPAddress,16000);

    // Create NatNet Client
    iResult = CreateClient(iConnectionType);
    if(iResult != ErrorCode_OK)
    {
        printf("Error initializing client.  See log for details.  Exiting");
        return 1;
    }
    else
    {
        printf("Client initialized and ready.\n");
    }


	// send/receive test request
	printf("[SampleClient] Sending Test Request\n");
	void* response;
	int nBytes;
	iResult = theClient->SendMessageAndWait("TestRequest", &response, &nBytes);
	if (iResult == ErrorCode_OK)
	{
		printf("[SampleClient] Received: %s", (char*)response);
	}

	// Retrieve Data Descriptions from server
	printf("\n\n[SampleClient] Requesting Data Descriptions...");
	sDataDescriptions* pDataDefs = NULL;
	int nBodies = theClient->GetDataDescriptions(&pDataDefs);
	if(!pDataDefs)
	{
		printf("[SampleClient] Unable to retrieve Data Descriptions.");
	}
	else
	{
        printf("[SampleClient] Received %d Data Descriptions:\n", pDataDefs->nDataDescriptions );
        for(int i=0; i < pDataDefs->nDataDescriptions; i++)
        {
            printf("Data Description # %d (type=%d)\n", i, pDataDefs->arrDataDescriptions[i].type);
            if(pDataDefs->arrDataDescriptions[i].type == Descriptor_MarkerSet)
            {
                // MarkerSet
                sMarkerSetDescription* pMS = pDataDefs->arrDataDescriptions[i].Data.MarkerSetDescription;
                printf("MarkerSet Name : %s\n", pMS->szName);
                for(int i=0; i < pMS->nMarkers; i++)
                    printf("%s\n", pMS->szMarkerNames[i]);

            }
            else if(pDataDefs->arrDataDescriptions[i].type == Descriptor_RigidBody)
            {
                // RigidBody
                sRigidBodyDescription* pRB = pDataDefs->arrDataDescriptions[i].Data.RigidBodyDescription;
                printf("RigidBody Name : %s\n", pRB->szName);
                printf("RigidBody ID : %d\n", pRB->ID);
                printf("RigidBody Parent ID : %d\n", pRB->parentID);
                printf("Parent Offset : %3.2f,%3.2f,%3.2f\n", pRB->offsetx, pRB->offsety, pRB->offsetz);
            }
            else if(pDataDefs->arrDataDescriptions[i].type == Descriptor_Skeleton)
            {
                // Skeleton
                sSkeletonDescription* pSK = pDataDefs->arrDataDescriptions[i].Data.SkeletonDescription;
                printf("Skeleton Name : %s\n", pSK->szName);
                printf("Skeleton ID : %d\n", pSK->skeletonID);
                printf("RigidBody (Bone) Count : %d\n", pSK->nRigidBodies);
                for(int j=0; j < pSK->nRigidBodies; j++)
                {
                    sRigidBodyDescription* pRB = &pSK->RigidBodies[j];
                    printf("  RigidBody Name : %s\n", pRB->szName);
                    printf("  RigidBody ID : %d\n", pRB->ID);
                    printf("  RigidBody Parent ID : %d\n", pRB->parentID);
                    printf("  Parent Offset : %3.2f,%3.2f,%3.2f\n", pRB->offsetx, pRB->offsety, pRB->offsetz);

                    // populate bone name dictionary for use in xml ==--
                    gBoneNames[pRB->ID] = pRB->szName;
                }
            }
            else
            {
                printf("Unknown data type.");
                // Unknown
            }
        }      
	}

	// Ready to receive marker stream!
	printf("\nClient is connected to server and listening for data...\n");
	int c;
	bool bExit = false;
	while(c =_getch())
	{
		switch(c)
		{
			case 'q':
				bExit = true;		
				break;	
			case 'r':
				resetClient();
				break;	
            case 'p':
                sServerDescription ServerDescription;
                memset(&ServerDescription, 0, sizeof(ServerDescription));
                theClient->GetServerDescription(&ServerDescription);
                if(!ServerDescription.HostPresent)
                {
                    printf("Unable to connect to server. Host not present. Exiting.");
                    return 1;
                }
                break;	
            case 'f':
                {
                    sFrameOfMocapData* pData = theClient->GetLastFrameOfData();
                    printf("Most Recent Frame: %d", pData->iFrame);
                }
                break;	
            case 'm':	                        // change to multicast
                iResult = CreateClient(ConnectionType_Multicast);
                if(iResult == ErrorCode_OK)
                    printf("Client connection type changed to Multicast.\n\n");
                else
                    printf("Error changing client connection type to Multicast.\n\n");
                break;
            case 'u':	                        // change to unicast
                iResult = CreateClient(ConnectionType_Unicast);
                if(iResult == ErrorCode_OK)
                    printf("Client connection type changed to Unicast.\n\n");
                else
                    printf("Error changing client connection type to Unicast.\n\n");
                break;


			default:
				break;
		}
		if(bExit)
			break;
	}

	// Done - clean up.
	theClient->Uninitialize();

	return ErrorCode_OK;
}

// Establish a NatNet Client connection
int CreateClient(int iConnectionType)
{
    // release previous server
    if(theClient)
    {
        theClient->Uninitialize();
        delete theClient;
    }

    // create NatNet client
    theClient = new NatNetClient(iConnectionType);

    // [optional] use old multicast group
    //theClient->SetMulticastAddress("224.0.0.1");

    // print version info
    unsigned char ver[4];
    theClient->NatNetVersion(ver);
    printf("NatNet Sample Client (NatNet ver. %d.%d.%d.%d)\n", ver[0], ver[1], ver[2], ver[3]);

    // Set callback handlers
    theClient->SetMessageCallback(MessageHandler);
    theClient->SetVerbosityLevel(Verbosity_Debug);
    theClient->SetDataCallback( DataHandler, theClient );	// this function will receive data from the server

    // Init Client and connect to NatNet server
    // to use NatNet default port assigments
    int retCode = theClient->Initialize(szMyIPAddress, szServerIPAddress);
    // to use a different port for commands and/or data:
    //int retCode = theClient->Initialize(szMyIPAddress, szServerIPAddress, MyServersCommandPort, MyServersDataPort);
    if (retCode != ErrorCode_OK)
    {
        printf("Unable to connect to server.  Error code: %d. Exiting", retCode);
        return ErrorCode_Internal;
    }
    else
    {
        // print server info
        sServerDescription ServerDescription;
        memset(&ServerDescription, 0, sizeof(ServerDescription));
        theClient->GetServerDescription(&ServerDescription);
        if(!ServerDescription.HostPresent)
        {
            printf("Unable to connect to server. Host not present. Exiting.");
            return 1;
        }
        printf("[SampleClient] Server application info:\n");
        printf("Application: %s (ver. %d.%d.%d.%d)\n", ServerDescription.szHostApp, ServerDescription.HostAppVersion[0],
            ServerDescription.HostAppVersion[1],ServerDescription.HostAppVersion[2],ServerDescription.HostAppVersion[3]);
        printf("NatNet Version: %d.%d.%d.%d\n", ServerDescription.NatNetVersion[0], ServerDescription.NatNetVersion[1],
            ServerDescription.NatNetVersion[2], ServerDescription.NatNetVersion[3]);
        printf("Client IP:%s\n", szMyIPAddress);
        printf("Server IP:%s\n", szServerIPAddress);
        printf("Server Name:%s\n\n", ServerDescription.szHostComputerName);
    }

    return ErrorCode_OK;

}

// Create XML from frame data and output to Unity
void SendFrameToUnity(sFrameOfMocapData *data, void *pUserData)
{
    if(data->Skeletons>0)
    {  
        // form XML document

        TiXmlDocument doc;  
        TiXmlDeclaration* decl = new TiXmlDeclaration( "1.0", "", "" );  
        doc.LinkEndChild( decl );  

        TiXmlElement * root = new TiXmlElement( "Stream" );  
        doc.LinkEndChild( root );
		root->SetAttribute("Frame", data->iFrame);
		root->SetDoubleAttribute("Time", data->fTimestamp);

        TiXmlElement * skeletons = new TiXmlElement( "Skeletons" );  
        root->LinkEndChild( skeletons );  

        // skeletons first

        for( int i=0; i<data->nSkeletons; i++ )
        {
            TiXmlElement * skeleton = new TiXmlElement( "Skeleton" );  
            skeletons->LinkEndChild( skeleton );  

            TiXmlElement * bone;

            sSkeletonData skData = data->Skeletons[i]; // first skeleton ==--

            skeleton->SetAttribute("ID"  , skData.skeletonID);

            for(int i=0; i<skData.nRigidBodies; i++)
            {
                sRigidBodyData rbData = skData.RigidBodyData[i];

                bone = new TiXmlElement( "Bone" );  
                skeleton->LinkEndChild( bone );  

                bone->SetAttribute      ("ID"  , rbData.ID);
                bone->SetAttribute      ("Name", gBoneNames[LOWORD(rbData.ID)].c_str());
                bone->SetDoubleAttribute("x"   , rbData.x);
                bone->SetDoubleAttribute("y"   , rbData.y);
                bone->SetDoubleAttribute("z"   , rbData.z);
                bone->SetDoubleAttribute("qx"  , rbData.qx);
                bone->SetDoubleAttribute("qy"  , rbData.qy);
                bone->SetDoubleAttribute("qz"  , rbData.qz);
                bone->SetDoubleAttribute("qw"  , rbData.qw);
            }
        }
        
        // rigid bodies ==--

        TiXmlElement * rigidBodies = new TiXmlElement( "RigidBodies" );  
        root->LinkEndChild( rigidBodies );  

        for( int i=0; i<data->nRigidBodies; i++ )
        {
            sRigidBodyData rbData = data->RigidBodies[i];

            TiXmlElement * rb = new TiXmlElement( "RigidBody" );  
            rigidBodies->LinkEndChild( rb );  

            rb->SetAttribute      ("ID"  , rbData.ID);
            rb->SetDoubleAttribute("x"   , rbData.x);
            rb->SetDoubleAttribute("y"   , rbData.y);
            rb->SetDoubleAttribute("z"   , rbData.z);
            rb->SetDoubleAttribute("qx"  , rbData.qx);
            rb->SetDoubleAttribute("qy"  , rbData.qy);
            rb->SetDoubleAttribute("qz"  , rbData.qz);
            rb->SetDoubleAttribute("qw"  , rbData.qw);
        }

        // convert xml document into a buffer filled with data ==--

        std::ostringstream stream;
        stream << doc;
        std::string str =  stream.str();
        const char* buffer = str.c_str();

        // stream xml data over UDP via SlipStream ==--

        gSlipStream->Stream( (unsigned char *) buffer, (int) strlen(buffer) );
    } 
}

// DataHandler receives data from the server
void __cdecl DataHandler(sFrameOfMocapData* data, void* pUserData)
{
	NatNetClient* pClient = (NatNetClient*) pUserData;

	printf("Received frame %d\n", data->iFrame);

    SendFrameToUnity(data, pUserData);
}

// MessageHandler receives NatNet error/debug messages
void __cdecl MessageHandler(int msgType, char* msg)
{
	printf("\n%s\n", msg);
}

void resetClient()
{
	int iSuccess;

	printf("\n\nre-setting Client\n\n.");

	iSuccess = theClient->Uninitialize();
	if(iSuccess != 0)
		printf("error un-initting Client\n");

	iSuccess = theClient->Initialize(szMyIPAddress, szServerIPAddress);
	if(iSuccess != 0)
		printf("error re-initting Client\n");


}

