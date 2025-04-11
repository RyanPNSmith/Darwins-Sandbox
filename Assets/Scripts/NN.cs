using UnityEngine;

public class NN : MonoBehaviour
{
    public int[] networkShape = {9, 32, 2};  // Fix at 9 inputs
    public Layer[] layers;
    private bool networkInitialized = false;
    public bool showDebugLogs = false;  // Control debug logging

    private void UpdateNetworkShape(int numInputs)
    {
        // Only update if absolutely necessary and network isn't initialized
        if (!networkInitialized)
        {
            numInputs = 9;  // Force to always use 9 inputs
            
            int oldInputs = networkShape != null && networkShape.Length > 0 ? networkShape[0] : 0;
            if (showDebugLogs) {
                Debug.Log($"Initializing neural network: [{numInputs}, 32, 2] (was [{oldInputs}, 32, 2])");
            }
            networkShape = new int[] { numInputs, 32, 2 };
            InitializeNetwork();
        }
    }
    
    private void InitializeNetwork()
    {
        // Initialize the layers
        layers = new Layer[networkShape.Length - 1];
        for (int i = 0; i < layers.Length; i++)
        {
            layers[i] = new Layer(networkShape[i], networkShape[i+1]);
        }
        networkInitialized = true;
        
        // This ensures that the random numbers we generate aren't the same pattern each time.
        Random.InitState((int)System.DateTime.Now.Ticks);
    }

    // Property to access and modify weights
    public float[] weights
    {
        get
        {
            // Calculate total number of weights
            int totalWeights = 0;
            for (int i = 0; i < layers.Length; i++)
            {
                totalWeights += layers[i].weightsArray.GetLength(0) * layers[i].weightsArray.GetLength(1);
            }

            // Create array to store all weights
            float[] allWeights = new float[totalWeights];
            int currentIndex = 0;

            // Copy weights from each layer
            for (int i = 0; i < layers.Length; i++)
            {
                int rows = layers[i].weightsArray.GetLength(0);
                int cols = layers[i].weightsArray.GetLength(1);
                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < cols; col++)
                    {
                        allWeights[currentIndex++] = layers[i].weightsArray[row, col];
                    }
                }
            }

            return allWeights;
        }
        set
        {
            int currentIndex = 0;
            for (int i = 0; i < layers.Length; i++)
            {
                int rows = layers[i].weightsArray.GetLength(0);
                int cols = layers[i].weightsArray.GetLength(1);
                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < cols; col++)
                    {
                        if (currentIndex < value.Length)
                        {
                            layers[i].weightsArray[row, col] = value[currentIndex++];
                        }
                    }
                }
            }
        }
    }

    // Awake is called when the script instance is being loaded.
    public void Awake()
    {
        // Check if network is already initialized
        if (networkInitialized) return;
        
        // Always use 9 inputs - fixed size to match TOTAL_INPUTS in Creature.cs
        int numInputs = 9;
        UpdateNetworkShape(numInputs);
    }
    
    private string GetNetworkShapeString()
    {
        if (networkShape == null || networkShape.Length == 0) return "[]";
        
        string result = "[";
        for (int i = 0; i < networkShape.Length; i++)
        {
            result += networkShape[i];
            if (i < networkShape.Length - 1) result += ", ";
        }
        result += "]";
        return result;
    }

    // This function is used to feed forward the inputs through the network and return the output
    public float[] Brain(float[] inputs)
    {
        // Make sure network is initialized
        if (!networkInitialized)
        {
            Awake();
        }
        
        // If input size doesn't match but network is already initialized, pad or truncate
        if (inputs.Length != networkShape[0])
        {
            float[] adjustedInputs = new float[networkShape[0]];
            
            // Copy as many inputs as possible
            for (int i = 0; i < Mathf.Min(inputs.Length, networkShape[0]); i++)
            {
                adjustedInputs[i] = inputs[i];
            }
            
            // Fill any remaining slots with 1.0 (max sensor distance)
            for (int i = inputs.Length; i < networkShape[0]; i++)
            {
                adjustedInputs[i] = 1.0f;
            }
            
            // Use adjusted inputs
            inputs = adjustedInputs;
            
            if (showDebugLogs) {
                Debug.Log($"Adjusted input array from size {inputs.Length} to {networkShape[0]}");
            }
        }

        for (int i = 0; i < layers.Length; i++)
        {
            if (i == 0)
            {
                layers[i].Forward(inputs);
                layers[i].Activation();
            } 
            else if (i == layers.Length - 1)
            {
                layers[i].Forward(layers[i - 1].nodeArray);
            }
            else
            {
                layers[i].Forward(layers[i - 1].nodeArray);
                layers[i].Activation();
            }    
        }

        return(layers[layers.Length - 1].nodeArray);
    }

    //This function is used to copy the weights and biases from one neural network to another.
    public Layer[] copyLayers()
    {
        Layer[] tmpLayers = new Layer[networkShape.Length - 1];
        for(int i = 0; i < layers.Length; i++)
        {
            tmpLayers[i] = new Layer(networkShape[i], networkShape[i+1]);
            System.Array.Copy (layers[i].weightsArray, tmpLayers[i].weightsArray, layers[i].weightsArray.GetLength(0)*layers[i].weightsArray.GetLength(1));
            System.Array.Copy (layers[i].biasesArray, tmpLayers[i].biasesArray, layers[i].biasesArray.GetLength(0));
        }
        return(tmpLayers);
    }

    public class Layer
    {
        //attributes, variables and properties of the class Layer
        public float[,] weightsArray;
        public float[] biasesArray;
        public float [] nodeArray;

        public int n_inputs;
        public int n_neurons;

        //constructor, this is called when we create a new layer, it sets the number of inputs and nodes, and creates the arrays.
        public Layer(int n_inputs, int n_neurons)
        {
            this.n_inputs = n_inputs;
            this.n_neurons = n_neurons;

            weightsArray = new float [n_neurons, n_inputs];
            biasesArray = new float[n_neurons];
        }

        //forward pass, takes in an array of inputs and returns an array of outputs, which is then used as the input for the next layer, and so on, until we get to the output layer, which is returned as the output of the network.
        public void Forward(float [] inputsArray)
        {
            nodeArray = new float [n_neurons];

            for(int i = 0;i < n_neurons ; i++)
            {
                //sum of weights times inputs
                for(int j = 0; j < n_inputs; j++)
                {
                    nodeArray[i] += weightsArray[i,j] * inputsArray[j];
                }

                //add the bias
                nodeArray[i] += biasesArray[i];
                
                // Special handling for hunting behavior in output layer
                if (i == 0 && n_neurons == 2) // This is the FB (forward/backward) output neuron
                {
                    // Default small bias for forward movement
                    nodeArray[i] += 0.2f;
                    
                    // If input includes prey info (assuming prey direction is input index numSensors+1)
                    if (inputsArray.Length >= 9 && inputsArray[inputsArray.Length-2] != 0)
                    {
                        // Apply a MUCH stronger forward movement bias when prey is detected
                        float preyDistanceInput = inputsArray[inputsArray.Length-1];
                        nodeArray[i] += 0.8f; // Strong base forward movement when prey detected
                        nodeArray[i] += preyDistanceInput * 0.5f; // Additional speed boost based on proximity
                    }
                }
                // For the turning neuron, add bias based on prey direction
                else if (i == 1 && n_neurons == 2 && inputsArray.Length >= 9)
                {
                    // If prey direction input exists
                    float preyDirection = inputsArray[inputsArray.Length-2];
                    if (preyDirection != 0)
                    {
                        // Apply a MUCH stronger turning bias in the direction of the prey
                        // The *0.9f was too strong and might be biasing one direction
                        // Use a more balanced approach to ensure both left and right turns work
                        nodeArray[i] += preyDirection * 0.7f;
                    }
                }
            }
        }

        //This function is the activation function for the neural network uncomment the one you want to use.
        public void Activation()
        {
            // Use only Tanh activation function for consistent behavior
            for(int i = 0; i < nodeArray.Length; i++)
            {
                nodeArray[i] = (float)System.Math.Tanh(nodeArray[i]);
            }
            
            /* COMMENTED OUT UNUSED ACTIVATION FUNCTIONS
            //leaky relu function
            for(int i = 0; i < nodeArray.Length; i++)
            {
                if(nodeArray[i] < 0)
                {
                    nodeArray[i] = nodeArray[i]/10;
                }
            }

            //sigmoid function
            for(int i = 0; i < nodeArray.Length; i++)
            {
                nodeArray[i] = 1/(1 + Mathf.Exp(-nodeArray[i]));
            }

            //relu function
            for(int i = 0; i < nodeArray.Length; i++)
            {
                if(nodeArray[i] < 0)
                {
                    nodeArray[i] = 0;
                }
            }
            */
        }

        //This is used to randomly modify the weights and biases for the Evolution Sim and Genetic Algorithm.
        public void MutateLayer(float mutationChance, float mutationAmount)
        {
            for(int i = 0; i < n_neurons; i++)
            {
                for(int j = 0; j < n_inputs; j++)
                {
                    if(Random.value < mutationChance)
                    {
                        weightsArray[i,j] += Random.Range(-1.0f, 1.0f)*mutationAmount;
                    }
                }

                if(Random.value < mutationChance)
                {
                    biasesArray[i] += Random.Range(-1.0f, 1.0f)*mutationAmount;
                }
            }
        }
    }


    //Call the randomness function for each layer in the network.
    public void MutateNetwork(float mutationChance, float mutationAmount)
    {
        for(int i = 0; i < layers.Length; i++)
        {
            layers[i].MutateLayer(mutationChance, mutationAmount);
        }
    }
}