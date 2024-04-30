import sys
import subprocess
import os
from plexapi.server import PlexServer

# Function to extract subtitles from a remote MKV file using ffmpeg
def extract_subtitles(plex_url, token, movie_title):
    plex = PlexServer(plex_url, token)
    library = plex.library
    section = library.section('Movies')
    movie = section.get(title=movie_title)
    if movie:
        movie.reload()  # Refresh movie data from the server
        for media in movie.media:
            for part in media.parts:
                remote_url = part.key  # Get the URL of the remote MKV file
                # Convert spaces in the movie title to underscores
                movie_title_no_spaces = movie_title.replace(' ', '_').replace(':', '')
                # Specify the output directory where subtitles will be saved
                output_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)), "subtitles")
                # Create the output directory if it doesn't exist
                os.makedirs(output_dir, exist_ok=True)
                print("Subtitle Streams:")
                count = 0
                for stream in part.subtitleStreams():
                    output_filename_srt = f"{movie_title_no_spaces}_subtitle_{count}.srt"
                    output_filename_sup = f"{movie_title_no_spaces}_subtitle_{count}.sup"
                    output_path_srt = os.path.join(output_dir, output_filename_srt)
                    output_path_sup = os.path.join(output_dir, output_filename_sup)
                    ffmpeg_command_srt = [
                        'ffmpeg',
                        '-i', plex_url + remote_url + "?X-Plex-Token=" + token,
                        '-map', f'0:s:{count}', output_path_srt
                    ]
                    try:
                        subprocess.run(ffmpeg_command_srt, check=True)
                        print(f"Subtitles extracted successfully for {movie_title}.")
                    except subprocess.CalledProcessError as e:
                        print(f"Error extracting subtitles as .srt: {e}")
                        # Try extracting as .sup
                        print(f"Extracting subtitles as .sup for {movie_title}... for {output_path_sup}")
                        ffmpeg_command_sup = [
                            'ffmpeg',
                            '-i', plex_url + remote_url + "?X-Plex-Token=" + token,
                            '-map', f'0:s:{count}', '-c', 'copy', output_path_sup
                        ]
                        try:
                            subprocess.run(ffmpeg_command_sup, check=True)
                            print(f"Subtitles extracted successfully for {movie_title}.")
                        except subprocess.CalledProcessError as e:
                            print(f"Error extracting subtitles as .sup: {e}")
                    count += 1
    else:
        print("Movie not found or no subtitles found for the specified movie.")

# Main function
def main():
    # Read Plex URL, token, and movie title from command-line arguments
    plex_url = sys.argv[1]
    token = sys.argv[2]
    movie_title = sys.argv[3]

    # Extract subtitles for the specified movie
    extract_subtitles(plex_url, token, movie_title)

if __name__ == "__main__":
    main()
