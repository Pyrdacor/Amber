using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

class Program
{
	static void Main(string[] args)
		{
				// Get input directory from args or ask the user
						string inputDirectory;
								if (args.Length > 0)
										{
													inputDirectory = args[0];
															}
																	else
																			{
																						Console.WriteLine("Please enter the input directory:");
																									inputDirectory = Console.ReadLine();
																											}

																													// Get output directory from args or ask the user
																															string outputDirectory;
																																	if (args.Length > 1)
																																			{
																																						outputDirectory = args[1];
																																								}
																																										else
																																												{
																																															Console.WriteLine("Please enter the output directory:");
																																																		outputDirectory = Console.ReadLine();
																																																				}

																																																						// Create the output directory if it doesn't exist
																																																								Directory.CreateDirectory(outputDirectory);

																																																										// Process all text files in the directory and subdirectories
																																																												ProcessDirectory(inputDirectory, outputDirectory);
																																																													}

																																																														static void ProcessDirectory(string inputDir, string outputDir)
																																																															{
																																																																	// Get all text files in the directory and subdirectories
																																																																			var txtFiles = Directory.GetFiles(inputDir, "*.txt", SearchOption.AllDirectories);

																																																																					// Dictionary to store unique words and their indices
																																																																							var wordIndexDict = new Dictionary<string, ushort>();

																																																																									foreach (var filePath in txtFiles)
																																																																											{
																																																																														// Read all lines from the file
																																																																																	var lines = File.ReadAllLines(filePath);

																																																																																				// List to store the word indices for the current file
																																																																																							var wordIndices = new List<ushort>();

																																																																																										foreach (var line in lines)
																																																																																													{
																																																																																																	// Split the line into words, including punctuation and newline
																																																																																																					var words = Regex.Split(line, @"(\s+|(?=\W)|(?<=\W))").Where(w => w != " ").ToList();

																																																																																																									foreach (var word in words)
																																																																																																													{
																																																																																																																		// Add newline as a separate word
																																																																																																																							if (word == "\n" || word == "\r\n")
																																																																																																																												{
																																																																																																																																		AddWordToDictionary("\n", wordIndexDict, wordIndices);
																																																																																																																																							}
																																																																																																																																												else
																																																																																																																																																	{
																																																																																																																																																							AddWordToDictionary(word, wordIndexDict, wordIndices);
																																																																																																																																																												}

																																																																																																																																																																	if (wordIndexDict.Count > ushort.MaxValue)
																																																																																																																																																																						{
																																																																																																																																																																												Console.WriteLine("Word count exceeded the limit of ushort.MaxValue");
																																																																																																																																																																																		return;
																																																																																																																																																																																							}
																																																																																																																																																																																											}
																																																																																																																																																																																														}

																																																																																																																																																																																																	// Create the output .dat file with the same name but different extension
																																																																																																																																																																																																				string outputFilePath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(filePath) + ".dat");
																																																																																																																																																																																																							WriteIndicesToFile(outputFilePath, wordIndices);
																																																																																																																																																																																																									}
																																																																																																																																																																																																										}

																																																																																																																																																																																																											static void AddWordToDictionary(string word, Dictionary<string, ushort> wordIndexDict, List<ushort> wordIndices)
																																																																																																																																																																																																												{
																																																																																																																																																																																																														if (!wordIndexDict.ContainsKey(word))
																																																																																																																																																																																																																{
																																																																																																																																																																																																																			wordIndexDict[word] = (ushort)wordIndexDict.Count;
																																																																																																																																																																																																																					}

																																																																																																																																																																																																																							wordIndices.Add(wordIndexDict[word]);
																																																																																																																																																																																																																								}

																																																																																																																																																																																																																									static void WriteIndicesToFile(string filePath, List<ushort> wordIndices)
																																																																																																																																																																																																																										{
																																																																																																																																																																																																																												using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
																																																																																																																																																																																																																														using (var writer = new BinaryWriter(stream))
																																																																																																																																																																																																																																{
																																																																																																																																																																																																																																			foreach (var index in wordIndices)
																																																																																																																																																																																																																																						{
																																																																																																																																																																																																																																										writer.Write(index);
																																																																																																																																																																																																																																													}
																																																																																																																																																																																																																																															}
																																																																																																																																																																																																																																																}
																																																																																																																																																																																																																																																}