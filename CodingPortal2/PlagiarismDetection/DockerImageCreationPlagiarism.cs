using Docker.DotNet;
using Docker.DotNet.Models;
namespace CodingPortal2.PlagiarismDetection
{
    public abstract class DockerImageCreationPlagiarism
    {
        public static async Task CreateDockerImage(DockerClient client)
        {
            string imageName = "ubuntu:20.04";

            var images = await client.Images.ListImagesAsync(new ImagesListParameters());

            var existingImage = images.FirstOrDefault(image => image.RepoTags != null && image.RepoTags.Contains(imageName));

            if (existingImage == null)
            {
                await client.Images.CreateImageAsync(
                    new ImagesCreateParameters
                    {
                        FromImage = imageName,
                    },
                    new AuthConfig(),
                    new Progress<JSONMessage>());
            }
        }
    }
}