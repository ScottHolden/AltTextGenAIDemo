# Alt Text GenAI Demo
A small example of building an API that can take in an image and generate alt-text for it, using supporting text to focus the description.

[![Deploy To Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FScottHolden%2FAltTextGenAIDemo%2Fmain%2Fdeploy%2Fdeploy.generated.json)

## How it works
1. An image is uploaded (or a URL to an image is provided)
1. The image is resized
1. A predefined prompt, the image, and optional supported text are passed to GPT-4o to generate alt-text
1. The alt-text (and optionally usage information) is returned to the user

## Prompt Engineering
The prompt is designed to be a simple, yet effective way to generate alt-text, and can be easily modified to suit different needs. The default prompt is:
```
You are an alt text generator for images. Given an image create a description of the image for a user who cannot see it.
You may also be provided with some supporting text to help you generate the alt text, if something is mentioned in the text, focus only on it in the alt text.
Keep all responses to a single sentence in length.
```
A text message is also added if any supporting text is provided. This is formatted as:
```
The image is related to: "{SupportingText}". Only mention this in the alt text, no other objects unrelated to this.
```

## Getting Started
_todo_