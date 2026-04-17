namespace CatshrediasNewsAPI.DTOs;

public record TagDto(int Id, string Name);

public record CreateTagDto(string Name);

public record UpdateTagSubscriptionsDto(List<int> TagIds);
